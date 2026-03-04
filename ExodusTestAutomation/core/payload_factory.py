"""
Swagger schema'sından gerçekçi ve tutarlı payload üretir.
Session state'ten ID'leri alarak birbiriyle uyumlu veri sağlar.
"""
import re
import time
from typing import Any, Dict, Optional
from core import session_state
from core.test_data import PRODUCTS, CATEGORIES, ADDRESSES, ADMIN, SELLER, CUSTOMER

def _ts() -> str:
    """Saniye cinsinden kısa timestamp - slug/SKU çakışmalarını önler."""
    return str(int(time.time()))[-6:]


class PayloadFactory:
    """Schema ve endpoint yoluna göre akıllı payload üretir."""

    def build(self, endpoint: Dict) -> Optional[Dict]:
        """Endpoint için payload üret. Body gerekmiyorsa None döner."""
        method = endpoint["method"]
        path = endpoint["path"]
        schema = endpoint.get("body_schema")

        if method in ("GET", "DELETE") and not schema:
            return None

        # Özel endpoint eşleştirme (öncelikli)
        custom = self._custom_payload(path, method)
        if custom is not None:
            return custom

        # Schema'dan genel payload üret
        if schema:
            return self._from_schema(schema, path)

        return None

    # Path prefix → session key eşlemesi: {id} parametresini doğru değerle doldurur
    _PATH_ID_MAP = [
        ("/attributes",      "created_attribute_id"),
        ("/values",          "created_value_id"),
        ("/banners",         "created_banner_id"),
        ("/campaigns",       "created_campaign_id"),
        ("/categories",      "created_category_id"),
        ("/brands",          "created_brand_id"),
        ("/products",        "created_product_id"),
        ("/listings",        "created_listing_id"),
        ("/orders",          "created_order_id"),
        ("/cart",            "created_cart_id"),
        ("/address",         "created_address_id"),
        ("/reviews",         "created_review_id"),
        ("/coupons",         "created_coupon_id"),
        ("/wishlist",        "created_wishlist_item_id"),
        ("/pages",           "created_page_id"),
        ("/affiliates",      "created_affiliate_id"),
        ("/users",           "created_user_id"),
        ("/notifications",   "created_notification_id"),
    ]

    def resolve_path_params(self, path: str) -> str:
        """Path'teki {id} gibi parametreleri session'dan doldur."""
        result = path
        path_lower = path.lower()

        # Spesifik isimli parametreler önce
        replacements = [
            (r"\{categoryId\}", str(session_state.get("created_category_id") or "1")),
            (r"\{productId\}", str(session_state.get("created_product_id") or "1")),
            (r"\{listingId\}", str(session_state.get("created_listing_id") or "1")),
            (r"\{orderId\}", str(session_state.get("created_order_id") or "1")),
            (r"\{cartId\}", str(session_state.get("created_cart_id") or "1")),
            (r"\{itemId\}", str(session_state.get("created_cart_item_id") or "1")),
            (r"\{addressId\}", str(session_state.get("created_address_id") or "1")),
            (r"\{reviewId\}", str(session_state.get("created_review_id") or "1")),
            (r"\{campaignId\}", str(session_state.get("created_campaign_id") or "1")),
            (r"\{couponId\}", str(session_state.get("created_coupon_id") or "1")),
            (r"\{sellerId\}", str(session_state.get("seller_user_id") or session_state.get("seller_id") or "1")),
            (r"\{userId\}", str(session_state.get("customer_id") or "1")),
            (r"\{brandId\}", str(session_state.get("created_brand_id") or "1")),
            (r"\{bannerId\}", str(session_state.get("created_banner_id") or "1")),
            (r"\{pageId\}", str(session_state.get("created_page_id") or "1")),
            (r"\{affiliateId\}", str(session_state.get("created_affiliate_id") or "1")),
            (r"\{attributeId\}", str(session_state.get("created_attribute_id") or "1")),
            (r"\{valueId\}", str(session_state.get("created_value_id") or "1")),
            (r"\{notificationId\}", str(session_state.get("created_notification_id") or "1")),
            (r"\{referralId\}", str(session_state.get("created_affiliate_id") or "1")),
            (r"\{payoutId\}", str(session_state.get("created_affiliate_id") or "1")),
        ]

        for pattern, value in replacements:
            result = re.sub(pattern, value, result, flags=re.IGNORECASE)

        # Genel {id} → path'e göre doğru session key'i bul
        if re.search(r"\{id\}", result, re.IGNORECASE):
            generic_id = "1"
            for segment, key in self._PATH_ID_MAP:
                if segment in path_lower:
                    val = session_state.get(key)
                    if val:
                        generic_id = str(val)
                    break
            result = re.sub(r"\{id\}", generic_id, result, flags=re.IGNORECASE)

        return result

    def _custom_payload(self, path: str, method: str) -> Optional[Dict]:
        """Endpoint'e özel payload'lar — doğrudan DTO sınıflarından türetildi."""
        path_lower = path.lower()

        # ── Auth ──────────────────────────────────────────────────────────────
        # RegisterDto: name, email, username, password, role
        if "/auth/register" in path_lower:
            return {
                "name": f"{CUSTOMER['firstName']} {CUSTOMER['lastName']}",
                "email": CUSTOMER["email"],
                "username": CUSTOMER["email"].split("@")[0],
                "password": CUSTOMER["password"],
                "role": CUSTOMER["role"],
            }

        # 2FA check önce (daha spesifik)
        if "/auth/login/2fa" in path_lower:
            return {"twoFactorCode": "000000"}

        if "/auth/login" in path_lower:
            return {
                "emailOrUsername": ADMIN["email"],
                "password": ADMIN["password"],
            }

        if "/auth/refresh" in path_lower:
            return {"refreshToken": "test-refresh-token"}

        if "/auth/change-password" in path_lower or "/auth/changepassword" in path_lower:
            return {
                "currentPassword": ADMIN["password"],
                "newPassword": "NewAdmin1234!",
                "confirmPassword": "NewAdmin1234!",
            }

        # ── Attributes ────────────────────────────────────────────────────────
        # CreateAttributeValueDto: value, code, colorHex(opt), imageUrl(opt), displayOrder
        if "/attributes" in path_lower and "/values" in path_lower and method == "POST":
            ts = _ts()
            return {
                "value": f"Kirmizi {ts}",
                "code": f"red-{ts}",
                "displayOrder": 0,
            }

        # UpdateAttributeValueDto: value, code, colorHex, isActive, displayOrder
        if "/attributes" in path_lower and "/values" in path_lower and method in ("PUT", "PATCH"):
            return {"value": "Guncellenen Deger", "isActive": True}

        # CreateAttributeDto: name, code, type(enum), isRequired, isFilterable, isVisibleOnProduct, displayOrder
        if "/attributes" in path_lower and method == "POST":
            ts = _ts()
            return {
                "name": f"Renk {ts}",
                "code": f"color-{ts}",
                "type": "Select",
                "isRequired": False,
                "isFilterable": True,
                "isVisibleOnProduct": True,
                "displayOrder": 0,
            }

        # UpdateAttributeDto: name, code, type, isFilterable, isActive, displayOrder
        if "/attributes" in path_lower and method in ("PUT", "PATCH"):
            return {"name": "Renk (Guncellendi)", "isActive": True, "isFilterable": True}

        # ── Banners ───────────────────────────────────────────────────────────
        # CreateBannerDto: title, description, imageUrl(required), mobileImageUrl,
        #                  targetUrl, position(enum), displayOrder, isActive, startDate, endDate
        # NOT: slug, linkUrl, subtitle, order alanları YOK
        if "/banners/reorder" in path_lower and method == "POST":
            banner_id = session_state.get("created_banner_id") or 1
            return {"bannerOrders": [{"id": banner_id, "order": 1}]}

        if "/banners" in path_lower and "/order" in path_lower and method in ("PATCH", "PUT"):
            return {"displayOrder": 1}

        if "/banners" in path_lower and method == "POST":
            return {
                "title": f"Kampanya Banner {_ts()}",
                "description": "Kacirılmayacak firsatlar",
                "imageUrl": "https://via.placeholder.com/1920x600",
                "targetUrl": "/kampanyalar",
                "position": "HomeSlider",
                "displayOrder": 1,
                "isActive": True,
            }

        if "/banners" in path_lower and method in ("PUT", "PATCH"):
            return {"title": "Guncellenen Banner", "isActive": True}

        # ── Content / Static Pages ────────────────────────────────────────────
        # CreatePageDto: title, slug(opt-auto), content(required), metaTitle, metaDescription,
        #                metaKeywords, isPublished, showInFooter, showInHeader, displayOrder,
        #                pageType(enum: General|Legal|Help|About|Landing)
        # NOT: isActive alanı YOK — sadece isPublished var
        if "/content/pages/reorder" in path_lower and method == "POST":
            page_id = session_state.get("created_page_id") or 1
            return {"pageOrders": [{"id": page_id, "order": 1}]}

        if "/content/pages" in path_lower and method == "POST":
            return {
                "title": f"Test Sayfasi {_ts()}",
                "content": "<p>Bu bir test icerigidir.</p>",
                "metaTitle": "Test Meta",
                "metaDescription": "Test meta aciklamasi",
                "isPublished": True,
                "showInFooter": False,
                "showInHeader": False,
                "displayOrder": 0,
                "pageType": "General",
            }

        if "/content/pages" in path_lower and method in ("PUT", "PATCH"):
            return {
                "title": "Guncellenen Sayfa",
                "content": "<p>Guncellennis icerik.</p>",
                "isPublished": True,
            }

        if "/content/seo" in path_lower and method == "PUT":
            return {
                "metaTitle": "Exodus Marketplace",
                "metaDescription": "En iyi alısveris deneyimi",
            }

        # ── Affiliates ────────────────────────────────────────────────────────
        # Admin: sadece PATCH endpointleri var (POST ile create yok)
        # UpdateAffiliateStatusDto: status(enum: Pending|Approved|Rejected|Suspended)
        if "/affiliates" in path_lower and "/status" in path_lower:
            return {"status": "Approved"}

        # UpdateAffiliateCommissionDto: commissionRate, minPayoutAmount
        if "/affiliates" in path_lower and "/commission" in path_lower:
            return {"commissionRate": 7.5, "minPayoutAmount": 100.0}

        # UpdateAffiliateBankInfoDto: bankName, iban, accountHolderName
        if "/affiliates" in path_lower and "/bank-info" in path_lower:
            return {
                "bankName": "Ziraat Bankasi",
                "iban": "TR000000000000000000000000",
                "accountHolderName": "Test Kullanici",
            }

        # CreateAffiliatePayoutDto: amount, notes
        if "/affiliates" in path_lower and "/payouts" in path_lower and method == "POST":
            return {"amount": 250.0, "notes": "Test odemesi"}

        # ProcessAffiliatePayoutDto: status, transferReference, notes
        if "/affiliates" in path_lower and "/payouts" in path_lower and method == "PATCH":
            return {"status": "Paid", "transferReference": "TRF-001", "notes": "Islem tamamlandi"}

        # UpdateReferralStatusDto: status(enum: Pending|Qualified|Approved|Paid|Cancelled)
        if "/affiliates/referrals" in path_lower and method == "PATCH":
            return {"status": "Approved"}

        # ── Categories ────────────────────────────────────────────────────────
        # CreateCategoryDto: name(required), description, imageUrl, parentCategoryId, displayOrder
        # NOT: isActive alanı create'de YOK
        if "/categories" in path_lower and method == "POST":
            return {
                "name": CATEGORIES[0]["name"],
                "description": CATEGORIES[0]["description"],
                "displayOrder": 0,
            }

        # UpdateCategoryDto: name, description, imageUrl, parentCategoryId, displayOrder, isActive
        if "/categories" in path_lower and method in ("PUT", "PATCH"):
            return {
                "name": "Elektronik Urunler",
                "description": "Guncel elektronik urunler",
                "isActive": True,
            }

        # ── Brands ────────────────────────────────────────────────────────────
        # CreateBrandDto: name(required), slug(opt-auto), description, logoUrl, bannerUrl,
        #                 website, isActive, isFeatured, displayOrder, metaTitle, metaDescription
        if "/brands/reorder" in path_lower and method == "POST":
            brand_id = session_state.get("created_brand_id") or 1
            return {"brandOrders": [{"id": brand_id, "order": 1}]}

        if "/brands" in path_lower and method == "POST":
            return {
                "name": f"TechBrand {_ts()}",
                "description": "Premium teknoloji markasi",
                "isActive": True,
                "isFeatured": False,
                "displayOrder": 0,
            }

        # UpdateBrandDto: tüm opsiyonel
        if "/brands" in path_lower and method in ("PUT", "PATCH"):
            return {"name": "TechBrand (Guncellendi)", "isActive": True}

        # ── Products ──────────────────────────────────────────────────────────
        # AddProductDto: productName, productDescription, barcodes[]
        # NOT: name, description, price, stock, sku, brand, categoryId alanları YOK
        if "/products" in path_lower and "/listings" not in path_lower and method == "POST":
            return {
                "productName": PRODUCTS[0]["name"],
                "productDescription": PRODUCTS[0]["description"],
                "barcodes": [f"BAR{_ts()}"],
            }

        # ProductUpdateDto: productName(required), productDescription(required), barcodes[]
        if "/products" in path_lower and "/listings" not in path_lower and method in ("PUT", "PATCH"):
            return {
                "productName": PRODUCTS[0]["name"] + " (Guncellendi)",
                "productDescription": PRODUCTS[0]["description"],
                "barcodes": [],
            }

        # ── Listings ──────────────────────────────────────────────────────────
        # AddListingDto: productId, sellerId, price, stock, condition(enum)
        # NOT: isActive alanı create'de YOK
        if "/listings" in path_lower and method == "POST":
            seller_id = (
                session_state.get("seller_user_id")
                or session_state.get("seller_id")
                or 1
            )
            return {
                "productId": session_state.get("created_product_id") or 1,
                "sellerId": seller_id,
                "price": PRODUCTS[0]["price"],
                "stock": 10,
                "condition": "New",
            }

        # UpdateListingDto: price, stock, condition, isActive
        if "/listings/bulk" in path_lower and method == "POST":
            return {
                "listingIds": [session_state.get("created_listing_id") or 1],
                "isActive": True,
            }

        if "/listings" in path_lower and method in ("PUT", "PATCH"):
            return {
                "price": round(PRODUCTS[0]["price"] * 0.9, 2),
                "stock": 8,
                "condition": "New",
                "isActive": True,
            }

        # ── Cart ──────────────────────────────────────────────────────────────
        # AddToCartDto: userId(required), listingId(required), quantity(required)
        if "/cart" in path_lower and method == "POST":
            return {
                "userId": session_state.get("customer_id") or 1,
                "listingId": session_state.get("created_listing_id") or 1,
                "quantity": 1,
            }

        # UpdateCartItemDto: quantity
        if "/cart" in path_lower and method in ("PUT", "PATCH"):
            return {"quantity": 2}

        if "/cart/coupon" in path_lower or ("/coupon" in path_lower and "/cart" in path_lower):
            return {"couponCode": "TEST10"}

        # ── Orders ────────────────────────────────────────────────────────────
        # CheckoutOrderDto: userId
        if "/orders/checkout" in path_lower and method == "POST":
            return {"userId": session_state.get("customer_id") or 1}

        # CreateOrderDto: shippingAddressId, billingAddressId(opt), customerNote(opt), couponCode(opt)
        # NOT: addressId, paymentMethod alanları YOK
        if "/orders" in path_lower and method == "POST":
            return {
                "shippingAddressId": session_state.get("created_address_id") or 1,
                "customerNote": "Lutfen kapi zili calismadigi icin telefon edin.",
            }

        if "/orders" in path_lower and "/status" in path_lower:
            return {"status": "Processing"}

        if "/orders" in path_lower and "/cancel" in path_lower:
            return {"reason": "CustomerRequest", "note": "Test iptali"}

        if "/orders" in path_lower and "/cargo" in path_lower:
            return {"cargoCompany": "Yurtici Kargo", "trackingNumber": "YK123456789TR"}

        if "/orders" in path_lower and "/refund" in path_lower:
            return {
                "reason": "Urun hasarli geldi",
                "description": "Kargo sirasinda zarar gormis",
            }

        # ── Addresses ─────────────────────────────────────────────────────────
        # CreateAddressDto: title, fullName, phone, city, district,
        #                   neighborhood(opt), addressLine, postalCode(opt), isDefault, type(enum)
        # NOT: firstName, lastName, street, buildingNo, zipCode alanları YOK
        if "/address" in path_lower and method == "POST":
            addr = ADDRESSES[0]
            return {
                "title": addr["title"],
                "fullName": f"{addr['firstName']} {addr['lastName']}",
                "phone": addr["phone"],
                "city": addr["city"],
                "district": addr["district"],
                "neighborhood": addr.get("neighborhood", ""),
                "addressLine": f"{addr['street']} No:{addr.get('buildingNo', '')}",
                "postalCode": addr.get("zipCode", "34710"),
                "isDefault": addr["isDefault"],
                "type": "Shipping",
            }

        # UpdateAddressDto: tüm opsiyonel, aynı alanlar
        if "/address" in path_lower and method in ("PUT", "PATCH"):
            return {"title": "Ev (Guncellendi)", "isDefault": True}

        # ── Reviews ───────────────────────────────────────────────────────────
        if "/reviews" in path_lower and method == "POST":
            return {
                "productId": session_state.get("created_product_id") or 1,
                "orderId": session_state.get("created_order_id") or 1,
                "rating": 5,
                "comment": "Harika bir urun, kesinlikle tavsiye ederim!",
            }

        if "/reviews" in path_lower and method in ("PUT", "PATCH"):
            return {"rating": 4, "comment": "Iyi urun"}

        # ── Campaigns ─────────────────────────────────────────────────────────
        # CreateCampaignDto: name(required), description, type(enum: PercentageDiscount|...|FreeShipping),
        #   sellerId(opt-int), startDate, endDate, isActive, discountPercentage(opt 0-100),
        #   discountAmount(opt), scope(enum: AllProducts|SpecificProducts|...), priority, isStackable
        # NOT: discountType, discountPercent alanları YOK
        if "/campaigns" in path_lower and "/products" in path_lower and method == "PUT":
            return {"productIds": [session_state.get("created_product_id") or 1]}

        if "/campaigns" in path_lower and "/categories" in path_lower and method == "PUT":
            return {"categoryIds": [session_state.get("created_category_id") or 1]}

        if "/campaigns" in path_lower and method == "POST":
            payload = {
                "name": f"Bahar Indirimi {_ts()}",
                "description": "Secili urunlerde %20 indirim",
                "type": "PercentageDiscount",
                "discountPercentage": 20.0,
                "startDate": "2026-01-01T00:00:00",
                "endDate": "2026-12-31T23:59:59",
                "isActive": True,
                "scope": "AllProducts",
                "priority": 0,
                "isStackable": False,
            }
            seller_uid = session_state.get("seller_user_id")
            if seller_uid:
                payload["sellerId"] = seller_uid
            return payload

        # UpdateCampaignDto: tüm opsiyonel
        if "/campaigns" in path_lower and method in ("PUT", "PATCH"):
            return {"name": "Guncellenen Kampanya", "isActive": True}

        # ── Coupons ───────────────────────────────────────────────────────────
        # Coupon = Campaign with couponCode + requiresCouponCode
        if "/coupons" in path_lower and method == "POST":
            ts = _ts()
            return {
                "name": f"Indirim Kuponu {ts}",
                "type": "PercentageDiscount",
                "discountPercentage": 10.0,
                "couponCode": f"EXODUS{ts}",
                "requiresCouponCode": True,
                "startDate": "2026-01-01T00:00:00",
                "endDate": "2026-12-31T23:59:59",
                "isActive": True,
                "scope": "AllProducts",
                "maxUsageCount": 100,
            }

        # ── Wishlist ──────────────────────────────────────────────────────────
        if "/wishlist" in path_lower and method == "POST":
            return {"productId": session_state.get("created_product_id") or 1}

        # ── Notifications ─────────────────────────────────────────────────────
        if "/notifications/send-bulk" in path_lower and method == "POST":
            return {
                "title": "Toplu Bildirim",
                "message": "Bu bir toplu test bildirimidir.",
                "userIds": [session_state.get("customer_id") or 1],
            }

        if "/notifications/delete-bulk" in path_lower and method == "POST":
            nid = session_state.get("created_notification_id") or 1
            return {"notificationIds": [nid]}

        if "/notifications" in path_lower and method == "POST":
            return {
                "userId": session_state.get("customer_id") or 1,
                "title": "Test Bildirimi",
                "message": "Bu bir test bildirimidir.",
            }

        # ── Settings ──────────────────────────────────────────────────────────
        if "/settings/bulk-update" in path_lower and method == "POST":
            return {"settings": [{"key": "test_key", "value": "test_value"}]}

        if "/settings" in path_lower and method in ("PUT", "PATCH"):
            return {"value": "test_value"}

        # ── Payment ───────────────────────────────────────────────────────────
        # CreatePaymentIntentDto: orderId, method(enum: CashOnDelivery|BankTransfer|CreditCard|...),
        #   currency(max 3), cardDetails{cardNumber,expiryDate,cvv,cardHolderName}, installmentCount, returnUrl
        # NOT: paymentMethod, card, amount alanları YOK
        _card = {
            "cardNumber": "4111111111111111",
            "expiryDate": "12/30",
            "cvv": "123",
            "cardHolderName": "Test Kullanici",
        }

        if "/payment/intents" in path_lower and method == "POST":
            return {
                "orderId": session_state.get("created_order_id") or 1,
                "method": "CreditCard",
                "currency": "TRY",
                "cardDetails": _card,
            }

        if "/payment/gateway/process" in path_lower and method == "POST":
            return {
                "orderId": session_state.get("created_order_id") or 1,
                "method": "CreditCard",
                "currency": "TRY",
                "cardDetails": _card,
            }

        if "/payment/gateway/3ds" in path_lower and method == "POST":
            return {
                "orderId": session_state.get("created_order_id") or 1,
                "method": "CreditCard",
                "currency": "TRY",
                "cardDetails": _card,
                "returnUrl": "https://localhost/callback",
            }

        # ── TwoFactor ─────────────────────────────────────────────────────────
        if "/twofactor" in path_lower and method == "POST":
            return {"code": "000000"}

        return None  # Özel payload yok, schema'dan üret

    def _from_schema(self, schema: Dict, path: str = "") -> Dict:
        """JSON schema'dan örnek payload üret."""
        schema_type = schema.get("type", "object")

        if schema_type == "object" or "properties" in schema:
            result = {}
            for prop_name, prop_schema in schema.get("properties", {}).items():
                result[prop_name] = self._generate_value(prop_name, prop_schema, path)
            return result

        return {}

    def _generate_value(self, name: str, schema: Dict, path: str = "") -> Any:
        """Property adı ve schema'ya göre gerçekçi değer üret."""
        prop_type = schema.get("type", "string")
        prop_format = schema.get("format", "")
        name_lower = name.lower()

        # Enum
        if "enum" in schema:
            return schema["enum"][0]

        # ID alanları — session'dan al
        if name_lower in ("categoryid",):
            return session_state.get("created_category_id") or 1
        if name_lower in ("productid",):
            return session_state.get("created_product_id") or 1
        if name_lower in ("listingid",):
            return session_state.get("created_listing_id") or 1
        if name_lower in ("orderid",):
            return session_state.get("created_order_id") or 1
        if name_lower in ("addressid", "shippingaddressid", "billingaddressid"):
            return session_state.get("created_address_id") or 1
        if name_lower in ("sellerid",):
            return session_state.get("seller_user_id") or session_state.get("seller_id") or 1
        if name_lower in ("userid",):
            return session_state.get("customer_id") or 1

        # String alanlar
        if prop_type == "string":
            if prop_format == "email" or "email" in name_lower:
                return CUSTOMER["email"]
            if prop_format == "password" or "password" in name_lower:
                return CUSTOMER["password"]
            if prop_format == "date-time" or "date" in name_lower:
                return "2026-06-01T10:00:00"
            if "phone" in name_lower or "tel" in name_lower:
                return "+905559876543"
            if "zip" in name_lower or "postal" in name_lower:
                return "34710"
            if "city" in name_lower:
                return "Istanbul"
            if "district" in name_lower:
                return "Kadikoy"
            if "addressline" in name_lower or "street" in name_lower:
                return "Moda Caddesi No:15"
            if "fullname" in name_lower:
                return "Test Kullanici"
            if "name" in name_lower:
                return "Test Urun"
            if "title" in name_lower:
                return "Test Basligi"
            if "description" in name_lower or "comment" in name_lower or "note" in name_lower:
                return "Bu bir test aciklamasidir."
            if "sku" in name_lower:
                return f"SKU-{_ts()}"
            if "slug" in name_lower:
                return f"slug-{_ts()}"
            if "code" in name_lower:
                return f"CODE-{_ts()}"
            if "brand" in name_lower:
                return "Sony"
            if "role" in name_lower:
                return "Customer"
            if "status" in name_lower:
                return "Active"
            if "tracking" in name_lower:
                return "YK123456789TR"
            if "url" in name_lower or "image" in name_lower:
                return "https://via.placeholder.com/400"
            return "Test degeri"

        # Number alanlar
        if prop_type in ("number", "integer"):
            if "price" in name_lower:
                return 999.99
            if "stock" in name_lower or "quantity" in name_lower or "count" in name_lower:
                return 10
            if "discount" in name_lower or "percent" in name_lower:
                return 10
            if "rating" in name_lower:
                return 5
            if "id" in name_lower:
                return 1
            return 1

        # Boolean
        if prop_type == "boolean":
            if "active" in name_lower or "enabled" in name_lower or "default" in name_lower:
                return True
            return False

        # Array
        if prop_type == "array":
            items_schema = schema.get("items", {})
            return [self._generate_value(name + "_item", items_schema, path)]

        return None
