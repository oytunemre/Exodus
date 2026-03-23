#!/usr/bin/env python3
"""
generated_payloads.json için düzeltme scripti.

Sorunlar:
1. productId/listingId state'de null → bilinen ID'lerle doldurur
2. auth_role yanlış atanmış → step_id prefix'e göre düzeltir
3. ticketId save_response eksik → ekler
4. on_conflict_get eksik → 409 alındığında mevcut kaynağı bulmak için GET path ekler

Kullanım:
    python fix_payloads.py
"""
import json
from pathlib import Path

PAYLOADS_FILE = Path(__file__).parent / "generated_payloads.json"

# 409 durumunda mevcut kaynağı bulmak için fallback GET path'leri
# key: step_id, value: GET path (state placeholder'ları desteklenir)
CONFLICT_GET_PATHS = {
    "admin_create_brand":        "/api/admin/brands",
    "admin_create_category":     "/api/admin/categories",
    # Ürün için /api/products kullan: AllowAnonymous, seller token ile /api/admin/products açılmaz (AdminOnly)
    "seller_create_product":     "/api/products",
    "seller_create_product_2":   "/api/products",
    # seller_create_listing → /api/listings (AddListingDto), fallback seller listings endpoint
    "seller_create_listing":     "/api/seller/listings",
    "seller_create_listing_2":   "/api/seller/listings",
    "seller_create_seller_listing": "/api/seller/listings",
    "admin_create_carrier":      "/api/admin/shipments/carriers",
    "admin_create_page":         "/api/admin/content/pages",
    "admin_delete_tax_rate":     None,  # Silme işlemi, fallback yok
}


def fix():
    with open(PAYLOADS_FILE, "r", encoding="utf-8") as f:
        data = json.load(f)

    # ── 1. Bilinen ID'leri state'e yaz ────────────────────────────────────
    known = {
        "productId": 9,
        "secondProductId": 10,
        "listingId": 9,
        "secondListingId": 10,
        "categoryId": 3,   # Mevcut "Elektronik" kategorisi
    }
    for k, v in known.items():
        if data["state"].get(k) is None:
            data["state"][k] = v
            print(f"  state.{k} = {v}")

    # ── 2. auth_role'ü step_id prefix'e göre düzelt ────────────────────────
    fixed_auth = 0
    for step in data["flow"]:
        if not step.get("requires_auth", False):
            continue

        step_id = step.get("id", "")
        if step_id.startswith("admin_"):
            correct = "admin"
        elif step_id.startswith("seller_"):
            correct = "seller"
        else:
            correct = "customer"

        current = step.get("auth_role", "customer")
        if current != correct:
            step["auth_role"] = correct
            fixed_auth += 1

    print(f"  {fixed_auth} adımın auth_role'ü düzeltildi")

    # ── 3. ticketId save_response düzelt ──────────────────────────────────
    for step in data["flow"]:
        if step.get("id") == "customer_create_support_ticket":
            sr = step.get("save_response", {})
            if "ticketId" not in sr:
                sr["ticketId"] = "ticketId"
                step["save_response"] = sr
                print("  customer_create_support_ticket save_response düzeltildi")

    # ── 4. comparisonId save_response düzelt ──────────────────────────────
    for step in data["flow"]:
        if step.get("id") == "customer_create_comparison":
            sr = step.get("save_response", {})
            if "comparisonId" not in sr:
                sr["comparisonId"] = "id"
                step["save_response"] = sr
                print("  customer_create_comparison save_response düzeltildi")

    # ── 5. on_conflict_get ekle/güncelle (409 fallback) ──────────────────────
    # Her zaman üzerine yaz — path değişmiş olabilir
    fixed_conflict = 0
    for step in data["flow"]:
        step_id = step.get("id", "")
        if step_id in CONFLICT_GET_PATHS:
            fallback = CONFLICT_GET_PATHS[step_id]
            if fallback and step.get("on_conflict_get") != fallback:
                step["on_conflict_get"] = fallback
                fixed_conflict += 1

    print(f"  {fixed_conflict} adımın on_conflict_get'i ayarlandı")

    # ── 6. Payload'larda boş Barcodes dizisini düzelt ─────────────────────
    # AddProductDto ve ProductUpdateDto Barcodes gerektirir (en az 1, min 3 char)
    fixed_barcodes = 0
    for step in data["flow"]:
        payload = step.get("payload", {})
        if isinstance(payload, dict) and "barcodes" in payload:
            barcodes = payload["barcodes"]
            if not isinstance(barcodes, list) or len(barcodes) == 0:
                payload["barcodes"] = ["BARCODE001"]
                fixed_barcodes += 1
        # Büyük harf key kontrolü
        if isinstance(payload, dict) and "Barcodes" in payload:
            barcodes = payload["Barcodes"]
            if not isinstance(barcodes, list) or len(barcodes) == 0:
                payload["Barcodes"] = ["BARCODE001"]
                fixed_barcodes += 1

    if fixed_barcodes:
        print(f"  {fixed_barcodes} adımın Barcodes alanı düzeltildi")

    # ── 7. Listing oluşturma adımları: /api/seller/listings'e taşı ──────────
    # SellerCreateListingDto: productId, price, stockQuantity, condition (SellerId YOK, JWT'den gelir)
    # AddListingDto: productId, sellerId, price, stock — SellerId > 0 zorunlu, cascade 400'e yol açar
    # Tüm listing oluşturma adımlarını /api/seller/listings'e taşıyarak SellerId bağımlılığını kaldır
    LISTING_CREATE_STEPS = {
        "seller_create_listing", "seller_create_listing_2", "seller_create_seller_listing"
    }
    fixed_listing = 0
    for step in data["flow"]:
        if step.get("id") not in LISTING_CREATE_STEPS:
            continue
        # Path düzelt
        if step.get("path", "") != "/api/seller/listings":
            step["path"] = "/api/seller/listings"
            fixed_listing += 1
        payload = step.get("payload", {})
        if isinstance(payload, dict):
            # stock → stockQuantity (SellerCreateListingDto alanı)
            for old, new in (("stock", "stockQuantity"), ("Stock", "StockQuantity")):
                if old in payload and new not in payload:
                    payload[new] = payload.pop(old)
            # SellerId JWT'den gelir, payload'da olmaz
            payload.pop("sellerId", None)
            payload.pop("SellerId", None)

    if fixed_listing:
        print(f"  {fixed_listing} listing oluşturma adımı → /api/seller/listings'e taşındı")

    # ── 8. Login save_response: UserId doğru kaydediliyor mu? ───────────────
    # AuthResponseDto: { UserId, Token, ... } — "id" field YOK, "UserId" var
    LOGIN_SAVE_MAP = {
        "login_admin":    {"adminToken": "token",  "adminId":    "UserId"},
        "login_seller":   {"sellerToken": "token", "sellerId":   "UserId"},
        "login_customer": {"customerToken": "token","customerId": "UserId"},
    }
    for step in data["flow"]:
        step_id = step.get("id", "")
        if step_id not in LOGIN_SAVE_MAP:
            continue
        expected = LOGIN_SAVE_MAP[step_id]
        sr = step.get("save_response", {})
        changed = False
        for state_key, resp_path in expected.items():
            if sr.get(state_key) != resp_path:
                sr[state_key] = resp_path
                changed = True
        if changed:
            step["save_response"] = sr
            print(f"  {step_id} save_response düzeltildi")

    # ── 9. seller_update_stock: SellerUpdateStockDto → stockQuantity ─────────
    for step in data["flow"]:
        if step.get("id") != "seller_update_stock":
            continue
        payload = step.get("payload", {})
        if isinstance(payload, dict):
            for old, new in (("stock", "stockQuantity"), ("Stock", "StockQuantity"),
                             ("quantity", "stockQuantity"), ("Quantity", "StockQuantity")):
                if old in payload and new not in payload:
                    payload[new] = payload.pop(old)
                    print("  seller_update_stock payload düzeltildi")

    # ── 10. customer_create_payment_intent: Method enum 1-7 ──────────────────
    # PaymentMethod: CashOnDelivery=1, BankTransfer=2, CreditCard=3, ...
    # 0 geçersiz, varsayılan CreditCard=3 ata
    for step in data["flow"]:
        if step.get("id") != "customer_create_payment_intent":
            continue
        payload = step.get("payload", {})
        if isinstance(payload, dict):
            method_key = next((k for k in ("method", "Method") if k in payload), None)
            if method_key:
                val = payload[method_key]
                if not isinstance(val, int) or not (1 <= val <= 7):
                    payload[method_key] = 3  # CreditCard
                    print("  customer_create_payment_intent method düzeltildi → 3 (CreditCard)")

    # ── 11. admin_create_home_widget: Type enum 0-10 ──────────────────────────
    # HomeWidgetType: Banner=0 ... FeaturedSellers=10
    for step in data["flow"]:
        if step.get("id") != "admin_create_home_widget":
            continue
        payload = step.get("payload", {})
        if isinstance(payload, dict):
            type_key = next((k for k in ("type", "Type") if k in payload), None)
            if type_key:
                val = payload[type_key]
                if not isinstance(val, int) or not (0 <= val <= 10):
                    payload[type_key] = 2  # ProductSlider
                    print("  admin_create_home_widget type düzeltildi → 2 (ProductSlider)")
            # name zorunlu
            name_key = next((k for k in ("name", "Name") if k in payload), None)
            if not name_key:
                payload["name"] = "Ana Sayfa Ürün Slider"
                print("  admin_create_home_widget name eklendi")

    # ── 12. admin_create_page: title ve content zorunlu ──────────────────────
    for step in data["flow"]:
        if step.get("id") != "admin_create_page":
            continue
        payload = step.get("payload", {})
        if isinstance(payload, dict):
            if not payload.get("title") and not payload.get("Title"):
                payload["title"] = "Hakkımızda"
                print("  admin_create_page title eklendi")
            if not payload.get("content") and not payload.get("Content"):
                payload["content"] = "<p>Exodus marketplace hakkında bilgi.</p>"
                print("  admin_create_page content eklendi")

    # ── 13. admin_create_carrier: name zorunlu ────────────────────────────────
    for step in data["flow"]:
        if step.get("id") != "admin_create_carrier":
            continue
        payload = step.get("payload", {})
        if isinstance(payload, dict):
            if not payload.get("name") and not payload.get("Name"):
                payload["name"] = "Aras Kargo"
                print("  admin_create_carrier name eklendi")

    # ── 14. customer_create_seller_review: Rating aralığı kontrolü ────────────
    for step in data["flow"]:
        if step.get("id") == "customer_create_seller_review":
            payload = step.get("payload", {})
            if isinstance(payload, dict):
                rating_key = next((k for k in ("rating", "Rating") if k in payload), None)
                if rating_key:
                    val = payload[rating_key]
                    if not isinstance(val, int) or not (1 <= val <= 5):
                        payload[rating_key] = 5
                        print("  customer_create_seller_review rating düzeltildi → 5")

    with open(PAYLOADS_FILE, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)

    print("\n✓ generated_payloads.json düzeltildi")
    print("  Şimdi çalıştır: python request_automation.py")


if __name__ == "__main__":
    fix()
