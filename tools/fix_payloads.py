#!/usr/bin/env python3
"""
generated_payloads.json için düzeltme scripti.

Sorunlar:
1. productId/listingId state'de null → bilinen ID'lerle doldurur
2. auth_role yanlış atanmış → step_id prefix'e göre düzeltir
3. ticketId save_response eksik → ekler

Kullanım:
    python fix_payloads.py
"""
import json
from pathlib import Path

PAYLOADS_FILE = Path(__file__).parent / "generated_payloads.json"


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

    with open(PAYLOADS_FILE, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)

    print("\n✓ generated_payloads.json düzeltildi")
    print("  Şimdi çalıştır: python request_automation.py")


if __name__ == "__main__":
    fix()
