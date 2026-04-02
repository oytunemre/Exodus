#!/usr/bin/env python3
"""
generated_payloads.json için düzeltme scripti.

Sorunlar:
1. productId/listingId state'de null → bilinen ID'lerle doldurur
2. auth_role yanlış atanmış → step_id prefix'e göre düzeltir
3. ticketId save_response eksik → ekler
4. Payment sandbox adımları → skip_if_state_null eklenir
5. Public endpoint'ler → state bağımlılıkları için skip_if_state_null eklenir

Kullanım:
    python fix_payloads.py
"""
import json
import re
from pathlib import Path

PAYLOADS_FILE = Path(__file__).parent / "generated_payloads.json"

# Gerçek ödeme gateway'i gerektiren adım ID'leri (sandbox olmadan çalışmaz)
SANDBOX_STEP_IDS = {
    "customer_process_gateway_payment",
    "customer_initialize_3ds",
    "customer_complete_3ds",
    "customer_check_bin",
    "customer_get_installments",
    "seller_process_gateway_payment",
}

# Gateway path'leri (sandbox required)
SANDBOX_PATH_PATTERNS = [
    r"/api/payment/gateway/",
    r"/api/payment/3ds",
    r"/api/payment/webhook/",
]

# State bağımlılığı: path pattern → gerekli state key
PATH_STATE_DEPS = {
    r"/api/categories/\{categoryId\}": "categoryId",
    r"/api/categories/\{categoryId\}/": "categoryId",
    r"/api/product-qa/\{productId\}": "productId",
    r"/api/product-qa/\{listingId\}": "listingId",
    r"/api/seller-reviews/\{sellerId\}": "sellerId",
    r"/api/sellers/\{sellerId\}": "sellerId",
    r"/api/products/\{productId\}": "productId",
    r"/api/listings/\{listingId\}": "listingId",
    r"/api/orders/\{orderId\}": "orderId",
    r"/api/payment/intents/\{paymentIntentId\}": "paymentIntentId",
}


def fix():
    with open(PAYLOADS_FILE, "r", encoding="utf-8") as f:
        data = json.load(f)

    # ── 1. Bilinen ID'leri state'e yaz ────────────────────────────────────
    # Not: categoryId'yi hardcode ETME — flow içinde oluşturulmuş olmalı.
    # Sadece null kalan secondary/optional state'leri doldur.
    print("\n[1] State kontrolü...")
    # Hiçbir hardcoded ID yok; flow'un kendi create adımlarından alması beklenir.
    print("  State hardcode atlandı — IDs will be captured from flow responses.")

    # ── 2. auth_role'ü step_id prefix'e göre düzelt ────────────────────────
    print("\n[2] auth_role düzeltiliyor...")
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
    print("\n[3] save_response eksiklikleri düzeltiliyor...")
    for step in data["flow"]:
        if step.get("id") == "customer_create_support_ticket":
            sr = step.get("save_response", {})
            if "ticketId" not in sr:
                sr["ticketId"] = "ticketId"
                step["save_response"] = sr
                print("  customer_create_support_ticket: ticketId eklendi")

    for step in data["flow"]:
        if step.get("id") == "customer_create_comparison":
            sr = step.get("save_response", {})
            if "comparisonId" not in sr:
                sr["comparisonId"] = "id"
                step["save_response"] = sr
                print("  customer_create_comparison: comparisonId eklendi")

    # ── 4. Payment sandbox adımlarına skip_if_state_null ekle ─────────────
    print("\n[4] Payment sandbox adımları işaretleniyor...")
    sandbox_marked = 0
    for step in data["flow"]:
        step_id = step.get("id", "")
        path = step.get("path", "")

        is_sandbox = step_id in SANDBOX_STEP_IDS
        if not is_sandbox:
            for pat in SANDBOX_PATH_PATTERNS:
                if re.search(pat, path):
                    is_sandbox = True
                    break

        if is_sandbox and "skip_if_state_null" not in step:
            # paymentIntentId olmadan bu adımlar zaten anlamsız; sandbox'a da gerek var
            step["skip_if_state_null"] = "_sandbox_disabled"
            step.setdefault("_note", "Requires payment sandbox — skipped in automation")
            sandbox_marked += 1

    print(f"  {sandbox_marked} sandbox adımı işaretlendi")

    # ── 5. Public endpoint'lere state bağımlılığı ekle ────────────────────
    print("\n[5] Public endpoint state bağımlılıkları ekleniyor...")
    dep_fixed = 0
    for step in data["flow"]:
        path = step.get("path", "")
        if "skip_if_state_null" in step:
            continue  # Zaten işaretli

        for path_pat, state_key in PATH_STATE_DEPS.items():
            # Path template'de bu parametre var mı?
            if re.search(path_pat.replace(r"\{", r"\{").replace(r"\}", r"\}"), path):
                step["skip_if_state_null"] = state_key
                dep_fixed += 1
                break

    print(f"  {dep_fixed} adıma state bağımlılığı eklendi")

    # ── 6. Register payload'larında role alanını zorla ────────────────────
    print("\n[6] Register role alanları düzeltiliyor...")
    role_fixed = 0
    role_map = {
        "register_admin":    "Admin",
        "register_seller":   "Seller",
        "register_customer": "Customer",
    }
    for step in data["flow"]:
        step_id = step.get("id", "")
        if step_id in role_map:
            payload = step.get("payload", {})
            expected_role = role_map[step_id]
            if payload.get("role") != expected_role:
                payload["role"] = expected_role
                step["payload"] = payload
                role_fixed += 1
                print(f"  {step_id}: role = {expected_role}")
    print(f"  {role_fixed} register adımının role'ü düzeltildi")

    # ── 7. Explicit login adımlarını token zaten varsa atla ───────────────
    print("\n[7] Explicit login adımları — token varsa atlama ekleniyor...")
    login_skip_map = {
        "login_admin":    "adminToken",
        "login_seller":   "sellerToken",
        "login_customer": "customerToken",
    }
    login_fixed = 0
    for step in data["flow"]:
        step_id = step.get("id", "")
        if step_id in login_skip_map:
            # skip_if_state_not_null yoksa ekle
            if "skip_if_state_not_null" not in step:
                step["skip_if_state_not_null"] = login_skip_map[step_id]
                login_fixed += 1
    print(f"  {login_fixed} explicit login adımına skip_if_state_not_null eklendi")

    # ── 8. _sandbox_disabled state key'ini None olarak başlat ─────────────
    # Bu key hiçbir zaman doldurulmaz → sandbox adımları her zaman atlanır
    if "_sandbox_disabled" not in data["state"]:
        data["state"]["_sandbox_disabled"] = None

    with open(PAYLOADS_FILE, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)

    flow_count = len(data["flow"])
    skippable = sum(1 for s in data["flow"] if "skip_if_state_null" in s)
    print(f"\n✓ generated_payloads.json düzeltildi")
    print(f"  Toplam adım       : {flow_count}")
    print(f"  Atlanabilir adım  : {skippable}")
    print(f"  Çalışacak adım    : {flow_count - skippable}")
    print("\n  Şimdi çalıştır: python request_automation.py")


if __name__ == "__main__":
    fix()
