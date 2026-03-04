"""
Satıcı (Seller) senaryosu:
Giriş → Ürün oluştur → Listing → Gelen siparişler → Sipariş yönet
"""
from scenarios.base_scenario import BaseScenario
from core import session_state
from core.test_data import SELLER, PRODUCTS


class SellerFlow(BaseScenario):
    def __init__(self):
        super().__init__("Satıcı Senaryosu (Seller Flow)")

    def run(self):
        print(f"\n  Senaryo: {self.name}")

        # 1. Giriş
        token = self.register_and_login(SELLER)
        login_step = {
            "name": "Satıcı girişi",
            "method": "POST",
            "path": "/api/auth/login",
            "success": token is not None,
            "skipped": False,
            "status_code": 200 if token else None,
            "response_time_ms": None,
            "error": None if token else "Seller login başarısız",
        }
        self.steps.append(login_step)
        if not token:
            self._failed_steps.add("Satıcı girişi")

        # 2. Profil görüntüle
        self.step(
            "Satıcı profilini görüntüle",
            "GET", "/api/seller/profile",
            token=token,
            depends_on=["Satıcı girişi"],
        )

        # 3. Ürün oluştur
        category_id = session_state.get("created_category_id") or 1
        product = PRODUCTS[0]
        self.step(
            "Yeni ürün oluştur",
            "POST", "/api/products",
            json={
                "name": product["name"],
                "description": product["description"],
                "price": product["price"],
                "stock": product["stock"],
                "sku": product["sku"],
                "brand": product["brand"],
                "categoryId": category_id,
                "isActive": True,
            },
            token=token,
            depends_on=["Satıcı girişi"],
            extract={"created_product_id": "id"},
        )

        # 4. Ürünleri listele
        self.step(
            "Kendi ürünlerini listele",
            "GET", "/api/products",
            token=token,
            depends_on=["Yeni ürün oluştur"],
        )

        # 5. Listing oluştur
        product_id = session_state.get("created_product_id") or 1
        self.step(
            "Listing oluştur",
            "POST", "/api/listings",
            json={
                "productId": product_id,
                "price": product["price"],
                "stock": 10,
                "isActive": True,
            },
            token=token,
            depends_on=["Yeni ürün oluştur"],
            extract={"created_listing_id": "id"},
        )

        # 6. Kampanya oluştur
        self.step(
            "Kampanya oluştur",
            "POST", "/api/campaigns",
            json={
                "name": "Bahar Fırsatı",
                "description": "Seçili ürünlerde %15 indirim",
                "discountPercent": 15,
                "startDate": "2025-03-01T00:00:00",
                "endDate": "2025-06-30T23:59:59",
                "isActive": True,
            },
            token=token,
            depends_on=["Satıcı girişi"],
        )

        # 7. Gelen siparişler
        self.step(
            "Gelen siparişleri görüntüle",
            "GET", "/api/seller/orders",
            token=token,
            depends_on=["Satıcı girişi"],
        )

        # 8. Sipariş durumu güncelle (varsa)
        order_id = session_state.get("created_order_id")
        if order_id:
            self.step(
                "Sipariş durumunu güncelle",
                "PUT", f"/api/seller/orders/{order_id}/status",
                json={"status": "Processing"},
                token=token,
                depends_on=["Gelen siparişleri görüntüle"],
            )

            self.step(
                "Kargo bilgisi gir",
                "PUT", f"/api/seller/orders/{order_id}/cargo",
                json={
                    "cargoCompany": "Yurtiçi Kargo",
                    "trackingNumber": "YK987654321TR",
                },
                token=token,
                depends_on=["Sipariş durumunu güncelle"],
            )

        # 9. Satıcı istatistikleri
        self.step(
            "Satış istatistiklerini gör",
            "GET", "/api/seller/stats",
            token=token,
            depends_on=["Satıcı girişi"],
        )

        # 10. Ödeme bilgilerini gör
        self.step(
            "Ödeme/payout bilgilerini gör",
            "GET", "/api/seller/payouts",
            token=token,
            depends_on=["Satıcı girişi"],
        )

        return self.get_result()
