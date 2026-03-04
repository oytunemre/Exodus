"""
Admin senaryosu:
Giriş → Kategori → Marka → Kullanıcı yönet → Siparişler → Dashboard
"""
from scenarios.base_scenario import BaseScenario
from core import session_state
from core.test_data import ADMIN, CATEGORIES


class AdminFlow(BaseScenario):
    def __init__(self):
        super().__init__("Admin Senaryosu (Admin Flow)")

    def run(self):
        print(f"\n  Senaryo: {self.name}")

        # 1. Admin girişi
        token = self.register_and_login(ADMIN)
        login_step = {
            "name": "Admin girişi",
            "method": "POST",
            "path": "/api/auth/login",
            "success": token is not None,
            "skipped": False,
            "status_code": 200 if token else None,
            "response_time_ms": None,
            "error": None if token else "Admin login başarısız",
        }
        self.steps.append(login_step)
        if not token:
            self._failed_steps.add("Admin girişi")

        # 2. Kategori oluştur
        self.step(
            "Kategori oluştur",
            "POST", "/api/admin/categories",
            json={
                "name": CATEGORIES[0]["name"],
                "description": CATEGORIES[0]["description"],
                "isActive": True,
            },
            token=token,
            depends_on=["Admin girişi"],
            extract={"created_category_id": "id"},
        )

        # 3. Kategorileri listele
        self.step(
            "Kategorileri listele",
            "GET", "/api/admin/categories",
            token=token,
            depends_on=["Admin girişi"],
        )

        # 4. Marka oluştur
        self.step(
            "Marka oluştur",
            "POST", "/api/admin/brands",
            json={
                "name": "ExodusBrand",
                "description": "Exodus platformu özel markası",
                "isActive": True,
            },
            token=token,
            depends_on=["Admin girişi"],
            extract={"created_brand_id": "id"},
        )

        # 5. Kullanıcıları listele
        self.step(
            "Tüm kullanıcıları listele",
            "GET", "/api/admin/users",
            token=token,
            depends_on=["Admin girişi"],
        )

        # 6. Satıcıları listele
        self.step(
            "Satıcıları listele",
            "GET", "/api/admin/sellers",
            token=token,
            depends_on=["Admin girişi"],
        )

        # 7. Ürünleri yönet
        self.step(
            "Tüm ürünleri görüntüle",
            "GET", "/api/admin/products",
            token=token,
            depends_on=["Admin girişi"],
        )

        # 8. Onay bekleyen ürünler
        self.step(
            "Onay bekleyen ürünleri listele",
            "GET", "/api/admin/products?status=PendingApproval",
            token=token,
            depends_on=["Admin girişi"],
        )

        # 9. Siparişleri görüntüle
        self.step(
            "Tüm siparişleri görüntüle",
            "GET", "/api/admin/orders",
            token=token,
            depends_on=["Admin girişi"],
        )

        # 10. Dashboard raporu
        self.step(
            "Dashboard istatistiklerini gör",
            "GET", "/api/admin/dashboard",
            token=token,
            depends_on=["Admin girişi"],
        )

        # 11. Site ayarları
        self.step(
            "Site ayarlarını görüntüle",
            "GET", "/api/admin/settings",
            token=token,
            depends_on=["Admin girişi"],
        )

        # 12. Kampanyaları yönet
        self.step(
            "Kampanyaları listele",
            "GET", "/api/admin/campaigns",
            token=token,
            depends_on=["Admin girişi"],
        )

        # 13. Kupon oluştur
        self.step(
            "Kupon oluştur",
            "POST", "/api/admin/coupons",
            json={
                "code": "EXODUS10",
                "discountPercent": 10,
                "minOrderAmount": 100,
                "maxUsage": 500,
                "expiryDate": "2025-12-31T23:59:59",
                "isActive": True,
            },
            token=token,
            depends_on=["Admin girişi"],
        )

        return self.get_result()
