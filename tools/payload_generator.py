#!/usr/bin/env python3
"""
Exodus Auto Payload Generator
==============================
Projeyi okuyup tüm endpointler için tutarlı JSON payload'ları oluşturur.

Kullanım:
    python payload_generator.py

Çıktı:
    generated_payloads.json  — request_automation.py tarafından kullanılır

Gereksinimler:
    pip install -r requirements.txt
    ANTHROPIC_API_KEY ortam değişkeni ayarlanmış olmalı
"""

import anthropic
import json
import os
import re
import sys
from pathlib import Path

PROJECT_ROOT = Path(__file__).parent.parent
CONTROLLERS_DIR = PROJECT_ROOT / "Controllers"
MODELS_DTO_DIR = PROJECT_ROOT / "Models" / "Dto"
OUTPUT_FILE = Path(__file__).parent / "generated_payloads.json"


# ─────────────────────────────────────────────────────────────────────────────
# 1. C# KAYNAK DOSYALARINI OKU
# ─────────────────────────────────────────────────────────────────────────────

def read_dto_files() -> str:
    """Tüm DTO dosyalarını okur ve birleştirir."""
    parts = []
    for path in sorted(MODELS_DTO_DIR.rglob("*.cs")):
        try:
            content = path.read_text(encoding="utf-8")
            rel = path.relative_to(PROJECT_ROOT)
            parts.append(f"=== {rel} ===\n{content}")
        except Exception as e:
            print(f"  [!] Okunamadı: {path.name} — {e}")
    print(f"  {len(parts)} DTO dosyası okundu")
    return "\n\n".join(parts)


def _extract_route_signature(controller_content: str) -> str:
    """Bir controller dosyasından sadece route + method imzalarını çıkarır.
    Tam implementasyon kodunu göndermek yerine sadece özet gönderiyoruz.
    """
    lines = []
    capture = False
    brace_depth = 0

    for line in controller_content.splitlines():
        stripped = line.strip()

        # Route attribute ve HTTP method attribute'larını yakala
        if re.match(r'\[Route\(|^\[Http(Get|Post|Put|Patch|Delete)|^\[Authorize|^\[Allow', stripped):
            lines.append(stripped)
            capture = True
            brace_depth = 0
            continue

        if capture:
            # Method imzasını yakala (public ... Async veya IAction...)
            if re.search(r'public\s+\w', stripped):
                lines.append(stripped)
                # Method signature aldıktan sonra dur
                capture = False
                lines.append("")  # boş satır
            elif stripped.startswith("//") or stripped == "":
                continue
            else:
                lines.append(stripped)

    return "\n".join(lines)


def read_controller_summaries() -> str:
    """Tüm controller dosyalarından route + method özetleri çıkarır."""
    parts = []
    for path in sorted(CONTROLLERS_DIR.rglob("*.cs")):
        try:
            content = path.read_text(encoding="utf-8")
            summary = _extract_route_signature(content)
            if summary.strip():
                rel = path.relative_to(PROJECT_ROOT)
                parts.append(f"=== {rel} ===\n{summary}")
        except Exception as e:
            print(f"  [!] Okunamadı: {path.name} — {e}")
    print(f"  {len(parts)} controller dosyası işlendi")
    return "\n\n".join(parts)


# ─────────────────────────────────────────────────────────────────────────────
# 2. CLAUDE İLE PAYLOAD ÜRET
# ─────────────────────────────────────────────────────────────────────────────

SYSTEM_PROMPT = """Sen bir ASP.NET Core API test uzmanısın.
Senden istenen şey: Bir marketplace projesinin endpoint'leri ve DTO'ları verildiğinde,
bu endpoint'ler için GERÇEKÇİ ve TUTARLI test flow'u oluşturmak.

TUTARLILIK KURALLARI (ZORUNLU):
1. Register payload'ındaki email/şifre, Login payload'ında BİREBİR AYNI olmalı.
2. Login'den dönen token → sonraki isteklerde {{adminToken}}/{{sellerToken}}/{{customerToken}} olarak kullanılmalı.
3. Adres oluşturulduktan sonra → order/checkout'ta {{addressId}} olarak kullanılmalı.
4. Ürün/listing oluşturulduktan sonra → cart/order'da {{listingId}} olarak kullanılmalı.
5. Order oluşturulduktan sonra → payment'ta {{orderId}} olarak kullanılmalı.
6. PUT/PATCH işlemlerindeki veriler POST ile oluşturulanlarla tutarlı olmalı.
7. Türkçe karakterler ve Türkiye'ye uygun gerçekçi veriler kullan (telefon: 5xxxxxxxxx formatında).
8. Şifreler güçlü olsun: büyük+küçük harf + rakam + özel karakter (en az 8 karakter).

STATE REFERANSLARI:
- {{adminToken}}, {{sellerToken}}, {{customerToken}} → JWT token
- {{adminId}}, {{sellerId}}, {{customerId}} → kullanıcı ID
- {{addressId}} → oluşturulan adres ID
- {{listingId}} → oluşturulan listing ID
- {{productId}} → oluşturulan ürün ID
- {{cartItemId}} → sepete eklenen item ID
- {{orderId}} → oluşturulan sipariş ID
- {{paymentIntentId}} → oluşturulan payment intent ID

SADECE geçerli JSON döndür, başka hiçbir şey ekleme."""

USER_PROMPT_TEMPLATE = """Aşağıdaki Exodus marketplace projesinin controller özetleri ve DTO'ları verilmiştir.

=== CONTROLLER ROUTE ÖZETLERİ ===
{controllers}

=== DTO DOSYALARI ===
{dtos}

Şimdi aşağıdaki EXACT JSON formatında bir test flow'u oluştur.
Tüm kritik endpoint'leri kapsayan tam bir flow oluştur:
Admin kayıt+login, Seller kayıt+login, Customer kayıt+login,
profil, adres, ürün, listing, sepet, sipariş, ödeme akışları dahil.

{{
  "variables": {{
    "baseUrl": "http://localhost:5013",
    "adminEmail": "admin@exodus.com",
    "adminPassword": "Admin123!@#",
    "adminName": "Ahmet Yılmaz",
    "adminUsername": "ahmetyilmaz",
    "sellerEmail": "satici@exodus.com",
    "sellerPassword": "Satici456!@#",
    "sellerName": "Mehmet Kaya",
    "sellerUsername": "mehmetkaya",
    "customerEmail": "musteri@exodus.com",
    "customerPassword": "Musteri789!@#",
    "customerName": "Ayşe Demir",
    "customerUsername": "aysedemir"
  }},
  "state": {{
    "_comment": "Bu alan request_automation.py tarafından runtime'da doldurulur",
    "adminToken": null,
    "sellerToken": null,
    "customerToken": null,
    "adminId": null,
    "sellerId": null,
    "customerId": null,
    "addressId": null,
    "billingAddressId": null,
    "productId": null,
    "listingId": null,
    "cartItemId": null,
    "orderId": null,
    "paymentIntentId": null,
    "campaignId": null
  }},
  "flow": [
    {{
      "id": "register_admin",
      "description": "Admin kullanıcı kaydı",
      "method": "POST",
      "path": "/api/auth/register",
      "requires_auth": false,
      "payload": {{
        "name": "{{{{adminName}}}}",
        "email": "{{{{adminEmail}}}}",
        "username": "{{{{adminUsername}}}}",
        "password": "{{{{adminPassword}}}}",
        "role": 0
      }},
      "save_response": {{}}
    }},
    ... (tüm diğer adımlar)
  ]
}}

ÖNEMLİ NOT: flow dizisindeki her adım için save_response alanı şu formatta olmalı:
- state'e kaydedilecek key → response içindeki JSON path (nokta ile ayrılmış)
- Örnek: {{"adminToken": "token", "adminId": "id"}}
- Yanıt doğruca dizi değilse root field'ı kullan."""


def generate_payloads(dto_content: str, controller_content: str) -> dict:
    """Claude Opus 4.6 ile endpoint payload'larını üretir. Streaming kullanır."""
    client = anthropic.Anthropic()

    prompt = USER_PROMPT_TEMPLATE.format(
        controllers=controller_content,
        dtos=dto_content,
    )

    print("  Model: claude-opus-4-6 (adaptive thinking + streaming)")

    full_text = ""
    with client.messages.stream(
        model="claude-opus-4-6",
        max_tokens=16000,
        thinking={"type": "adaptive"},
        system=SYSTEM_PROMPT,
        messages=[{"role": "user", "content": prompt}],
    ) as stream:
        for text in stream.text_stream:
            full_text += text
            print(".", end="", flush=True)

    print()  # newline after dots

    # JSON bloğunu temizle
    text = full_text.strip()
    if "```json" in text:
        text = text.split("```json", 1)[1].split("```", 1)[0].strip()
    elif "```" in text:
        text = text.split("```", 1)[1].split("```", 1)[0].strip()

    return json.loads(text)


# ─────────────────────────────────────────────────────────────────────────────
# 3. MAIN
# ─────────────────────────────────────────────────────────────────────────────

def main() -> None:
    print("=" * 60)
    print("  Exodus Auto Payload Generator")
    print("=" * 60)

    # API key kontrolü
    if not os.environ.get("ANTHROPIC_API_KEY"):
        print("\n[HATA] ANTHROPIC_API_KEY ortam değişkeni tanımlı değil.")
        print("  export ANTHROPIC_API_KEY='sk-ant-...'")
        sys.exit(1)

    print(f"\nProje dizini: {PROJECT_ROOT}")

    print("\n[1/3] Controller route'ları okunuyor...")
    controller_content = read_controller_summaries()

    print("\n[2/3] DTO dosyaları okunuyor...")
    dto_content = read_dto_files()

    print(f"\n[3/3] Claude ile payload'lar üretiliyor...")
    print("  (Streaming, birkaç saniye sürebilir...)")

    try:
        payloads = generate_payloads(dto_content, controller_content)
    except json.JSONDecodeError as e:
        print(f"\n[HATA] JSON parse hatası: {e}")
        print("  Yanıt geçerli JSON içermiyor. Lütfen tekrar deneyin.")
        sys.exit(1)
    except anthropic.APIError as e:
        print(f"\n[HATA] Claude API hatası: {e}")
        sys.exit(1)

    # Kaydet
    OUTPUT_FILE.parent.mkdir(parents=True, exist_ok=True)
    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        json.dump(payloads, f, ensure_ascii=False, indent=2)

    flow_count = len(payloads.get("flow", []))
    print(f"\n✓ Payload'lar kaydedildi: {OUTPUT_FILE}")
    print(f"✓ Toplam flow adımı: {flow_count}")
    print("\nSonraki adım:")
    print("  python request_automation.py")


if __name__ == "__main__":
    main()
