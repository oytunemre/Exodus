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

    # ── 5. on_conflict_get ekle (409 fallback) ────────────────────────────
    fixed_conflict = 0
    for step in data["flow"]:
        step_id = step.get("id", "")
        if step_id in CONFLICT_GET_PATHS:
            fallback = CONFLICT_GET_PATHS[step_id]
            if fallback and not step.get("on_conflict_get"):
                step["on_conflict_get"] = fallback
                fixed_conflict += 1

    print(f"  {fixed_conflict} adıma on_conflict_get eklendi")

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

    # ── 7. seller_create_seller_listing: alan adı düzelt ────────────────────
    # /api/seller/listings → SellerCreateListingDto: stockQuantity (int), SellerId YOK (JWT'den gelir)
    # /api/listings        → AddListingDto:           stock (int), SellerId gerekli (validator: > 0)
    # seller_create_listing ve seller_create_listing_2 büyük ihtimalle /api/listings kullanır → dokunma
    fixed_listing = 0
    for step in data["flow"]:
        if step.get("id") == "seller_create_seller_listing":
            payload = step.get("payload", {})
            if isinstance(payload, dict):
                # "stock" → "stockQuantity"
                if "stock" in payload and "stockQuantity" not in payload:
                    payload["stockQuantity"] = payload.pop("stock")
                    fixed_listing += 1
                if "Stock" in payload and "StockQuantity" not in payload:
                    payload["StockQuantity"] = payload.pop("Stock")
                    fixed_listing += 1
                # SellerId is taken from JWT for /api/seller/listings
                payload.pop("sellerId", None)
                payload.pop("SellerId", None)

    if fixed_listing:
        print(f"  {fixed_listing} seller_create_seller_listing payload'ı düzeltildi (stock→stockQuantity)")

    # ── 8. customer_create_seller_review: Rating aralığı kontrolü ─────────
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
