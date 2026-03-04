"""
Alıcı (Customer) senaryosu:
Kayıt → Giriş → Ürünlere bak → Favorilere ekle → Sepete ekle → Sipariş
"""
from scenarios.base_scenario import BaseScenario
from core import session_state
from core.test_data import CUSTOMER, ADDRESSES


class BuyerFlow(BaseScenario):
    def __init__(self):
        super().__init__("Alıcı Senaryosu (Buyer Flow)")

    def run(self):
        print(f"\n  Senaryo: {self.name}")

        # 1. Kayıt & Giriş
        token = self.register_and_login(CUSTOMER)
        login_step = {
            "name": "Kayıt ol & Giriş yap",
            "method": "POST",
            "path": "/api/auth/login",
            "success": token is not None,
            "skipped": False,
            "status_code": 200 if token else None,
            "response_time_ms": None,
            "error": None if token else "Login başarısız",
        }
        self.steps.append(login_step)
        if not token:
            self._failed_steps.add("Kayıt ol & Giriş yap")

        # 2. Ürünlere bak
        self.step(
            "Ürün listesini gör",
            "GET", "/api/products",
            token=token,
            depends_on=["Kayıt ol & Giriş yap"],
        )

        # 3. Kategorileri gör
        self.step(
            "Kategorileri gör",
            "GET", "/api/categories",
            token=token,
        )

        # 4. Kategoriye göre filtrele
        cat_id = session_state.get("created_category_id") or 1
        self.step(
            "Kategoriye göre filtrele",
            "GET", f"/api/products?categoryId={cat_id}",
            token=token,
        )

        # 5. Adres ekle
        self.step(
            "Teslimat adresi ekle",
            "POST", "/api/addresses",
            json={
                "title": ADDRESSES[0]["title"],
                "firstName": ADDRESSES[0]["firstName"],
                "lastName": ADDRESSES[0]["lastName"],
                "phone": ADDRESSES[0]["phone"],
                "city": ADDRESSES[0]["city"],
                "district": ADDRESSES[0]["district"],
                "street": ADDRESSES[0]["street"],
                "buildingNo": ADDRESSES[0]["buildingNo"],
                "zipCode": ADDRESSES[0]["zipCode"],
                "isDefault": True,
            },
            token=token,
            depends_on=["Kayıt ol & Giriş yap"],
            extract={"created_address_id": "id"},
        )

        # 6. Ürünü favorilere ekle
        product_id = session_state.get("created_product_id") or 1
        self.step(
            "Ürünü favorilere ekle",
            "POST", "/api/wishlist",
            json={"productId": product_id},
            token=token,
            depends_on=["Kayıt ol & Giriş yap"],
        )

        # 7. Sepete ekle
        listing_id = session_state.get("created_listing_id") or 1
        self.step(
            "Ürünü sepete ekle",
            "POST", "/api/cart/add",
            json={"listingId": listing_id, "quantity": 1},
            token=token,
            depends_on=["Kayıt ol & Giriş yap"],
        )

        # 8. Sepeti görüntüle
        self.step(
            "Sepeti görüntüle",
            "GET", "/api/cart",
            token=token,
            depends_on=["Ürünü sepete ekle"],
        )

        # 9. Sipariş ver
        address_id = session_state.get("created_address_id") or 1
        self.step(
            "Sipariş oluştur (checkout)",
            "POST", "/api/orders/checkout",
            json={
                "addressId": address_id,
                "paymentMethod": "CreditCard",
                "note": "Kapı zili çalışmıyor, lütfen arayın.",
            },
            token=token,
            depends_on=["Ürünü sepete ekle"],
            expected_status=200,
            extract={"created_order_id": "id"},
        )

        # 10. Siparişi görüntüle
        order_id = session_state.get("created_order_id") or 1
        self.step(
            "Siparişi görüntüle",
            "GET", f"/api/orders/{order_id}",
            token=token,
            depends_on=["Sipariş oluştur (checkout)"],
        )

        # 11. Tüm siparişlerimi gör
        self.step(
            "Tüm siparişlerimi listele",
            "GET", "/api/orders",
            token=token,
            depends_on=["Kayıt ol & Giriş yap"],
        )

        # 12. Ürün değerlendir
        self.step(
            "Ürüne yorum yaz",
            "POST", "/api/reviews",
            json={
                "productId": product_id,
                "orderId": session_state.get("created_order_id") or 1,
                "rating": 5,
                "comment": "Harika ürün, hızlı kargo!",
            },
            token=token,
            depends_on=["Sipariş oluştur (checkout)"],
        )

        return self.get_result()
