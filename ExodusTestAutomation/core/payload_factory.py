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
        """Endpoint'e özel önceden tanımlanmış payload'lar."""
        path_lower = path.lower()

        # Auth
        if "/auth/register" in path_lower:
            return {
                "email": CUSTOMER["email"],
                "password": CUSTOMER["password"],
                "name": f"{CUSTOMER['firstName']} {CUSTOMER['lastName']}",
                "username": CUSTOMER["email"].split("@")[0],
                "firstName": CUSTOMER["firstName"],
                "lastName": CUSTOMER["lastName"],
                "role": CUSTOMER["role"],
                "phoneNumber": CUSTOMER.get("phoneNumber", "+905559876543"),
            }

        if "/auth/login" in path_lower:
            return {
                "emailOrUsername": ADMIN["email"],
                "password": ADMIN["password"],
            }

        if "/auth/login/2fa" in path_lower:
            return {"twoFactorCode": "000000"}

        if "/auth/refresh" in path_lower:
            return {"refreshToken": "test-refresh-token"}

        if "/auth/change-password" in path_lower or "/auth/changepassword" in path_lower:
            return {
                "currentPassword": ADMIN["password"],
                "newPassword": "NewAdmin1234!",
                "confirmPassword": "NewAdmin1234!",
            }

        # Attributes
        if "/attributes" in path_lower and "/values" in path_lower and method == "POST":
            return {
                "value": f"Kırmızı_{_ts()}",
                "code": f"red-{_ts()}",
                "isActive": True,
            }

        if "/attributes" in path_lower and method == "POST":
            return {
                "name": f"Renk_{_ts()}",
                "code": f"color-{_ts()}",
                "type": "Select",
                "isActive": True,
            }

        if "/attributes" in path_lower and method in ("PUT", "PATCH"):
            return {
                "name": "Renk (Güncellendi)",
                "isActive": True,
            }

        # Banners
        if "/banners" in path_lower and method == "POST":
            ts = _ts()
            return {
                "title": f"Kampanya Banner {ts}",
                "subtitle": "Kaçırılmayacak fırsatlar",
                "imageUrl": "https://via.placeholder.com/1920x600",
                "linkUrl": f"/kampanya-{ts}",
                "slug": f"banner-{ts}",
                "isActive": True,
                "order": 1,
            }

        if "/banners" in path_lower and method in ("PUT", "PATCH"):
            return {
                "title": "Güncellenen Banner",
                "isActive": True,
            }

        # Content / Pages
        if "/pages" in path_lower and method == "POST":
            ts = _ts()
            return {
                "title": f"Test Sayfası {ts}",
                "slug": f"test-sayfa-{ts}",
                "content": "<p>Bu bir test içeriğidir.</p>",
                "metaTitle": f"Test Sayfası {ts}",
                "metaDescription": "Test meta açıklaması",
                "isActive": True,
                "isPublished": True,
            }

        if "/pages" in path_lower and method in ("PUT", "PATCH"):
            return {
                "title": "Güncellenen Sayfa",
                "content": "<p>Güncellenmiş içerik.</p>",
                "isActive": True,
            }

        # Affiliates
        if "/affiliates" in path_lower and method == "POST":
            ts = _ts()
            return {
                "name": f"Affiliate Partner {ts}",
                "email": f"affiliate{ts}@test.com",
                "commissionRate": 5.0,
                "isActive": True,
            }

        if "/affiliates" in path_lower and method in ("PUT", "PATCH"):
            return {
                "name": "Güncellenen Partner",
                "commissionRate": 7.5,
                "isActive": True,
            }

        # Categories
        if "/categories" in path_lower and method == "POST":
            return {
                "name": CATEGORIES[0]["name"],
                "description": CATEGORIES[0]["description"],
                "isActive": True,
            }

        if "/categories" in path_lower and method in ("PUT", "PATCH"):
            return {
                "name": "Elektronik Ürünler",
                "description": "Güncel elektronik ürünler ve aksesuarlar",
                "isActive": True,
            }

        # Brands
        if "/brands" in path_lower and method == "POST":
            return {
                "name": f"TechBrand_{_ts()}",
                "description": "Premium teknoloji markası",
                "isActive": True,
            }

        # Products
        if "/products" in path_lower and method == "POST":
            product = PRODUCTS[0]
            return {
                "name": product["name"],
                "description": product["description"],
                "price": product["price"],
                "stock": product["stock"],
                "sku": f"{product['sku']}-{_ts()}",
                "brand": product["brand"],
                "categoryId": session_state.get("created_category_id") or 1,
                "isActive": True,
            }

        if "/products" in path_lower and method in ("PUT", "PATCH"):
            return {
                "name": PRODUCTS[0]["name"] + " (Güncellendi)",
                "description": PRODUCTS[0]["description"],
                "price": PRODUCTS[0]["price"] * 0.9,
                "stock": PRODUCTS[0]["stock"],
                "isActive": True,
            }

        # Listings
        if "/listings" in path_lower and method == "POST":
            return {
                "productId": session_state.get("created_product_id") or 1,
                "price": PRODUCTS[0]["price"],
                "stock": 10,
                "isActive": True,
            }

        # Cart
        if "/cart/add" in path_lower or ("/cart" in path_lower and "/items" in path_lower and method == "POST"):
            payload = {
                "listingId": session_state.get("created_listing_id") or 1,
                "quantity": 1,
            }
            customer_id = session_state.get("customer_id")
            if customer_id:
                payload["userId"] = customer_id
            return payload

        if "/cart/coupon" in path_lower or "/coupon" in path_lower:
            return {"couponCode": "EXODUS10"}

        # Orders
        if "/orders/checkout" in path_lower or ("/orders" in path_lower and method == "POST"):
            return {
                "addressId": session_state.get("created_address_id") or 1,
                "paymentMethod": "CreditCard",
                "note": "Lütfen kapı zili çalışmadığı için telefon edin.",
            }

        if "/orders" in path_lower and "/status" in path_lower:
            return {"status": "Processing"}

        if "/orders" in path_lower and "/cargo" in path_lower:
            return {
                "cargoCompany": "Yurtiçi Kargo",
                "trackingNumber": "YK123456789TR",
            }

        # Addresses
        if "/addresses" in path_lower and method == "POST":
            addr = ADDRESSES[0]
            # /api/Profile/addresses farklı DTO kullanıyor
            if "/profile/" in path_lower:
                return {
                    "fullName": f"{addr['firstName']} {addr['lastName']}",
                    "addressLine": f"{addr['street']} No:{addr['buildingNo']} Daire:{addr['apartmentNo']}",
                    "city": addr["city"],
                    "district": addr["district"],
                    "zipCode": addr["zipCode"],
                    "phone": addr["phone"],
                    "title": addr["title"],
                    "isDefault": addr["isDefault"],
                }
            return {
                "title": addr["title"],
                "firstName": addr["firstName"],
                "lastName": addr["lastName"],
                "phone": addr["phone"],
                "city": addr["city"],
                "district": addr["district"],
                "neighborhood": addr.get("neighborhood", ""),
                "street": addr["street"],
                "buildingNo": addr["buildingNo"],
                "zipCode": addr["zipCode"],
                "isDefault": addr["isDefault"],
            }

        # Reviews
        if "/reviews" in path_lower and method == "POST":
            return {
                "productId": session_state.get("created_product_id") or 1,
                "orderId": session_state.get("created_order_id") or 1,
                "rating": 5,
                "comment": "Harika bir ürün, kesinlikle tavsiye ederim!",
            }

        # Seller Campaigns (farklı DTO)
        if "/seller/campaigns" in path_lower and method == "POST":
            return {
                "name": f"Satici Kampanya {_ts()}",
                "description": "Seçili ürünlerde indirim",
                "discountType": "Percentage",
                "discountPercent": 15,
                "startDate": "2026-01-01T00:00:00",
                "endDate": "2026-12-31T23:59:59",
                "isActive": True,
            }

        # Admin Campaigns - SellerId gönderilmemeli (null/FK ihlali önlenir)
        if "/campaigns" in path_lower and method == "POST":
            payload = {
                "name": f"Bahar İndirimi {_ts()}",
                "description": "Seçili ürünlerde %20 indirim",
                "discountType": "Percentage",
                "discountPercent": 20,
                "startDate": "2026-01-01T00:00:00",
                "endDate": "2026-12-31T23:59:59",
                "isActive": True,
            }
            seller_user_id = session_state.get("seller_user_id")
            if seller_user_id:
                payload["sellerId"] = seller_user_id
            return payload

        # Campaign products/categories update
        if "/campaigns" in path_lower and "/products" in path_lower and method == "PUT":
            return {"productIds": [session_state.get("created_product_id") or 1]}

        if "/campaigns" in path_lower and "/categories" in path_lower and method == "PUT":
            return {"categoryIds": [session_state.get("created_category_id") or 1]}

        # Coupons
        if "/coupons" in path_lower and method == "POST":
            return {
                "code": f"EXODUS{_ts()}",
                "discountPercent": 10,
                "minOrderAmount": 100,
                "maxUsage": 100,
                "expiryDate": "2026-12-31T23:59:59",
                "isActive": True,
            }

        # Wishlist
        if "/wishlist" in path_lower and method == "POST":
            return {
                "productId": session_state.get("created_product_id") or 1,
            }

        # Notifications
        if "/notifications/send" in path_lower and method == "POST":
            return {
                "title": "Test Bildirimi",
                "message": "Bu bir test bildirimidir.",
                "userId": session_state.get("customer_id") or 1,
            }

        if "/notifications/delete-bulk" in path_lower and method == "POST":
            return {"notificationIds": [1]}

        if "/notifications/send-bulk" in path_lower and method == "POST":
            return {
                "title": "Toplu Bildirim",
                "message": "Bu bir toplu test bildirimidir.",
                "userIds": [session_state.get("customer_id") or 1],
            }

        # Settings
        if "/settings/bulk-update" in path_lower and method == "POST":
            return {"settings": [{"key": "test_key", "value": "test_value"}]}

        if "/settings" in path_lower and "/settings/" in path_lower and method == "PUT":
            return {"value": "test_value"}

        if "/settings" in path_lower and method in ("PUT", "PATCH"):
            return {
                "siteName": "Exodus Marketplace",
                "supportEmail": "destek@exodus.com",
            }

        # Payment
        if "/payment/intents" in path_lower and method == "POST":
            return {
                "orderId": session_state.get("created_order_id") or 1,
                "amount": 100.0,
                "currency": "TRY",
                "paymentMethod": "CreditCard",
            }

        if "/payment/gateway/process" in path_lower and method == "POST":
            return {
                "orderId": session_state.get("created_order_id") or 1,
                "amount": 100.0,
                "currency": "TRY",
                "card": {
                    "cardNumber": "4111111111111111",
                    "cardHolderName": "Test User",
                    "expireMonth": "12",
                    "expireYear": "2030",
                    "cvc": "123",
                },
            }

        if "/payment/gateway/3ds/initialize" in path_lower and method == "POST":
            return {
                "orderId": session_state.get("created_order_id") or 1,
                "amount": 100.0,
                "currency": "TRY",
                "card": {
                    "cardNumber": "4111111111111111",
                    "cardHolderName": "Test User",
                    "expireMonth": "12",
                    "expireYear": "2030",
                    "cvc": "123",
                },
                "callbackUrl": "https://localhost/callback",
            }

        # TwoFactor
        if "/twofactor/verify" in path_lower or "/twofactor/disable" in path_lower or "/twofactor/backup-codes" in path_lower:
            return {"code": "000000"}

        # Listings bulk update
        if "/listings/bulk-update" in path_lower and method == "POST":
            return {
                "listingIds": [session_state.get("created_listing_id") or 1],
                "isActive": True,
            }

        # Admin notifications send
        if "/admin/notifications/send" in path_lower and not "bulk" in path_lower and method == "POST":
            return {
                "userId": session_state.get("customer_id") or 1,
                "title": "Test Bildirimi",
                "message": "Bu bir test bildirimidir.",
            }

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
        if name_lower in ("addressid",):
            return session_state.get("created_address_id") or 1

        # String alanlar
        if prop_type == "string":
            if prop_format == "email" or "email" in name_lower:
                return CUSTOMER["email"]
            if prop_format == "password" or "password" in name_lower:
                return CUSTOMER["password"]
            if prop_format == "date-time" or "date" in name_lower:
                return "2025-06-01T10:00:00"
            if "phone" in name_lower or "tel" in name_lower:
                return "+905559876543"
            if "zip" in name_lower or "postal" in name_lower:
                return "34710"
            if "city" in name_lower:
                return "İstanbul"
            if "district" in name_lower:
                return "Kadıköy"
            if "address" in name_lower or "street" in name_lower:
                return "Moda Caddesi No:15"
            if "name" in name_lower:
                return "Test Ürün"
            if "title" in name_lower:
                return "Test Başlığı"
            if "description" in name_lower or "comment" in name_lower or "note" in name_lower:
                return "Bu bir test açıklamasıdır."
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
            return "Test değeri"

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
