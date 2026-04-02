#!/usr/bin/env python3
"""
Exodus Request Automation
==========================
payload_generator.py tarafından üretilen generated_payloads.json dosyasını
kullanarak Exodus API'sine sıralı ve state-aware HTTP istekleri atar.

Kullanım:
    python request_automation.py                    # Tüm flow'u çalıştır
    python request_automation.py --step register_admin  # Tek adım
    python request_automation.py --from login_admin     # Belirli adımdan başla
    python request_automation.py --list             # Adımları listele

Gereksinimler:
    pip install -r requirements.txt
    generated_payloads.json dosyası mevcut olmalı (önce payload_generator.py çalıştır)
"""

import argparse
import json
import sys
import time
from pathlib import Path
from typing import Any, Optional

import requests

PAYLOADS_FILE = Path(__file__).parent / "generated_payloads.json"
RESULTS_FILE = Path(__file__).parent / "automation_results.json"


# ─────────────────────────────────────────────────────────────────────────────
# YARDIMCI FONKSİYONLAR
# ─────────────────────────────────────────────────────────────────────────────

def _resolve(value: Any, variables: dict, state: dict) -> Any:
    """{{placeholder}} referanslarını variables + state ile değiştirir."""
    if isinstance(value, str):
        # Tüm {{...}} referanslarını çöz
        import re
        def replace_ref(m: re.Match) -> str:
            key = m.group(1)
            if key in state and state[key] is not None:
                return str(state[key])
            if key in variables:
                return str(variables[key])
            return m.group(0)  # Çözülemediyse olduğu gibi bırak
        return re.sub(r'\{\{(\w+)\}\}', replace_ref, value)

    if isinstance(value, dict):
        return {k: _resolve(v, variables, state) for k, v in value.items()}

    if isinstance(value, list):
        return [_resolve(item, variables, state) for item in value]

    return value


def _get_nested(data: Any, path: str) -> Optional[Any]:
    """'a.b.c' formatındaki path ile iç içe dict'ten değer çeker."""
    keys = path.split(".")
    current = data
    for key in keys:
        if isinstance(current, dict):
            current = current.get(key)
        else:
            return None
        if current is None:
            return None
    return current


def _resolve_path_params(path: str, variables: dict, state: dict) -> str:
    """URL path'indeki {id}, {{id}} parametrelerini state'den doldurur."""
    import re
    def replace_path_param(m: re.Match) -> str:
        key = m.group(1)
        # Sayısal literal ise direkt kullan: {13} → 13
        if key.isdigit():
            return key
        # Önce state, sonra variables'a bak
        if key in state and state[key] is not None:
            return str(state[key])
        if key in variables:
            return str(variables[key])
        return m.group(0)
    # Önce {{key}} çift parantezi işle, sonra {key} tek parantezi
    result = re.sub(r'\{\{(\w+)\}\}', replace_path_param, path)
    result = re.sub(r'\{(\w+)\}', replace_path_param, result)
    return result


def _color(text: str, code: str) -> str:
    """Terminal renk kodları (desteklenmiyorsa düz metin)."""
    if not sys.stdout.isatty():
        return text
    return f"\033[{code}m{text}\033[0m"


def _status_icon(status_code: int) -> str:
    if status_code == 0:
        return _color("✗ BAĞLANTI", "91")
    if status_code < 300:
        return _color(f"✓ {status_code}", "92")
    if status_code < 400:
        return _color(f"→ {status_code}", "93")
    return _color(f"✗ {status_code}", "91")


# ─────────────────────────────────────────────────────────────────────────────
# ANA SINIF
# ─────────────────────────────────────────────────────────────────────────────

class ExodusAutomation:
    def __init__(self, payloads_file: Path = PAYLOADS_FILE, delay: float = 0.4):
        if not payloads_file.exists():
            print(f"[HATA] {payloads_file} bulunamadı.")
            print("  Önce payload generator'ı çalıştır: python payload_generator.py")
            sys.exit(1)

        with open(payloads_file, "r", encoding="utf-8") as f:
            self.config = json.load(f)

        self.variables: dict = self.config.get("variables", {})
        self.state: dict = dict(self.config.get("state", {}))
        self.flow: list[dict] = self.config.get("flow", [])
        self.base_url: str = self.variables.get("baseUrl", "http://localhost:5013")
        self.delay = delay
        self.session = requests.Session()
        self.results: list[dict] = []

    # ── İSTEK ATAN ANA METOD ────────────────────────────────────────────────

    def run_step(self, step: dict) -> dict:
        step_id = step.get("id", "?")
        description = step.get("description", step_id)
        method = step.get("method", "GET").upper()
        path = step.get("path", "/")
        requires_auth = step.get("requires_auth", False)
        auth_role = step.get("auth_role", "customer")
        raw_payload = step.get("payload", {})
        save_response = step.get("save_response", {})
        skip_if_null = step.get("skip_if_state_null", None)
        skip_if_not_null = step.get("skip_if_state_not_null", None)

        # State bağımlılık kontrolü — null ise atla
        if skip_if_null and self.state.get(skip_if_null) is None:
            print(f"\n  [{step_id}]  → ATLANDI (state.{skip_if_null} = null)")
            result = {"step_id": step_id, "skipped": True, "reason": f"state.{skip_if_null} is null"}
            self.results.append(result)
            return result

        # State bağımlılık kontrolü — null değilse atla (token zaten var)
        if skip_if_not_null and self.state.get(skip_if_not_null) is not None:
            print(f"\n  [{step_id}]  → ATLANDI (state.{skip_if_not_null} zaten mevcut)")
            result = {"step_id": step_id, "skipped": True, "reason": f"state.{skip_if_not_null} already set"}
            self.results.append(result)
            return result

        # Path parametrelerini çöz
        resolved_path = _resolve_path_params(path, self.variables, self.state)

        # Çözülemeyen path parametresi varsa adımı atla
        import re as _re
        if _re.search(r'\{[a-zA-Z]\w*\}', resolved_path):
            unresolved = _re.findall(r'\{([a-zA-Z]\w*)\}', resolved_path)
            print(f"\n  [{step_id}]  → ATLANDI (çözülemeyen path parametresi: {unresolved})")
            result = {"step_id": step_id, "skipped": True, "reason": f"unresolved path params: {unresolved}"}
            self.results.append(result)
            return result

        url = f"{self.base_url}{resolved_path}"

        # Payload'ı çöz
        resolved_payload = _resolve(raw_payload, self.variables, self.state)

        # Headers
        headers: dict = {"Content-Type": "application/json", "Accept": "application/json"}
        if requires_auth:
            token_key = f"{auth_role}Token"
            token = self.state.get(token_key)
            if token:
                headers["Authorization"] = f"Bearer {token}"
            else:
                print(f"  [{step_id}]  ⚠ {auth_role}Token state'de yok, auth header eklenmedi")

        # Ekrana yaz
        print(f"\n{'─'*60}")
        print(f"  [{step_id}]  {_color(method, '96')} {resolved_path}")
        print(f"  {description}")
        if resolved_payload:
            print(f"  Payload: {json.dumps(resolved_payload, ensure_ascii=False)}")

        # İsteği gönder
        try:
            response = self.session.request(
                method=method,
                url=url,
                json=resolved_payload if resolved_payload else None,
                headers=headers,
                timeout=30,
            )

            try:
                response_data = response.json()
            except Exception:
                response_data = response.text

            # 409 Conflict on register → force-verify (dev endpoint) then login fallback
            if response.status_code == 409 and "/auth/register" in resolved_path:
                email = resolved_payload.get("email", resolved_payload.get("emailOrUsername", ""))
                password = resolved_payload.get("password", "")
                role = resolved_payload.get("role")  # e.g. "Admin", "Seller", "Customer"
                print(f"  → 409 Conflict (kullanıcı zaten var), email verify + login deneniyor...")
                try:
                    # Step 1: Force-verify + fix role via dev endpoint (idempotent)
                    verify_body = {"email": email}
                    if role:
                        verify_body["role"] = role
                    self.session.post(
                        f"{self.base_url}/api/auth/dev/force-verify",
                        json=verify_body,
                        headers={"Content-Type": "application/json", "Accept": "application/json"},
                        timeout=10,
                    )
                    # Step 2: Login with the same credentials
                    login_payload = {"emailOrUsername": email, "password": password}
                    login_resp = self.session.post(
                        f"{self.base_url}/api/auth/login",
                        json=login_payload,
                        headers={"Content-Type": "application/json", "Accept": "application/json"},
                        timeout=30,
                    )
                    if login_resp.status_code < 400:
                        login_data = login_resp.json()
                        if save_response and isinstance(login_data, dict):
                            for state_key, resp_path in save_response.items():
                                value = _get_nested(login_data, resp_path)
                                if value is not None:
                                    self.state[state_key] = value
                                    print(f"  → state.{state_key} = {value} (login fallback)")
                        response_data = login_data
                        result = {
                            "step_id": step_id,
                            "method": method,
                            "url": url,
                            "payload": resolved_payload,
                            "status_code": login_resp.status_code,
                            "success": True,
                            "response": login_data,
                            "note": "409 conflict — force-verify + login fallback kullanıldı",
                        }
                        print(f"  {_color('✓ login fallback başarılı', '92')}")
                        self.results.append(result)
                        return result
                    else:
                        print(f"  ⚠ Login fallback da başarısız: HTTP {login_resp.status_code}")
                        try:
                            print(f"  ⚠ Yanıt: {login_resp.json()}")
                        except Exception:
                            print(f"  ⚠ Yanıt: {login_resp.text[:200]}")
                except Exception as e:
                    print(f"  ⚠ Login fallback exception: {e}")

            # Başarılı register → force-verify + role fix (yeni kayıt da hazır olsun)
            if response.status_code < 400 and "/auth/register" in resolved_path:
                email = resolved_payload.get("email", "")
                role = resolved_payload.get("role")
                if email:
                    try:
                        verify_body = {"email": email}
                        if role:
                            verify_body["role"] = role
                        self.session.post(
                            f"{self.base_url}/api/auth/dev/force-verify",
                            json=verify_body,
                            headers={"Content-Type": "application/json"},
                            timeout=10,
                        )
                    except Exception:
                        pass  # Dev endpoint yoksa görmezden gel

            # State'e değerleri kaydet
            if save_response and isinstance(response_data, dict) and response.status_code < 400:
                for state_key, response_path in save_response.items():
                    value = _get_nested(response_data, response_path)
                    if value is not None:
                        self.state[state_key] = value
                        print(f"  → state.{state_key} = {value}")

            result = {
                "step_id": step_id,
                "method": method,
                "url": url,
                "payload": resolved_payload,
                "status_code": response.status_code,
                "success": response.status_code < 400,
                "response": response_data,
            }

            print(f"  {_status_icon(response.status_code)}", end="")
            if isinstance(response_data, dict) and "message" in response_data:
                print(f"  {response_data['message']}")
            else:
                print()

        except requests.exceptions.ConnectionError:
            print(f"  {_color('✗ BAĞLANTI HATASI', '91')} — {self.base_url} erişilemiyor")
            print(f"  Sunucu çalışıyor mu? dotnet run komutunu kontrol et.")
            result = {
                "step_id": step_id,
                "method": method,
                "url": url,
                "status_code": 0,
                "success": False,
                "error": "Connection refused",
            }

        except requests.exceptions.Timeout:
            print(f"  {_color('✗ TIMEOUT', '91')}")
            result = {
                "step_id": step_id,
                "method": method,
                "url": url,
                "status_code": 0,
                "success": False,
                "error": "Request timed out",
            }

        self.results.append(result)
        return result

    # ── FLOW ÇALIŞTIRICI ─────────────────────────────────────────────────────

    def run_all(self) -> list[dict]:
        """Tüm flow'u sırayla çalıştırır."""
        return self._run_steps(self.flow)

    def run_from(self, step_id: str) -> list[dict]:
        """Belirtilen adımdan itibaren çalıştırır."""
        idx = next((i for i, s in enumerate(self.flow) if s.get("id") == step_id), None)
        if idx is None:
            print(f"[HATA] '{step_id}' adımı bulunamadı.")
            self.list_steps()
            sys.exit(1)
        return self._run_steps(self.flow[idx:])

    def run_single(self, step_id: str) -> list[dict]:
        """Tek bir adımı çalıştırır."""
        step = next((s for s in self.flow if s.get("id") == step_id), None)
        if step is None:
            print(f"[HATA] '{step_id}' adımı bulunamadı.")
            self.list_steps()
            sys.exit(1)
        return self._run_steps([step])

    def _run_steps(self, steps: list[dict]) -> list[dict]:
        print(f"\n{'='*60}")
        print(f"  Exodus Request Automation")
        print(f"  Base URL : {self.base_url}")
        print(f"  Adım sayısı: {len(steps)}")
        print(f"{'='*60}")

        for step in steps:
            self.run_step(step)
            if self.delay > 0:
                time.sleep(self.delay)

        self._print_summary()
        self._save_results()
        return self.results

    # ── ÖZET & KAYDET ────────────────────────────────────────────────────────

    def _print_summary(self) -> None:
        total = len(self.results)
        skipped = sum(1 for r in self.results if r.get("skipped"))
        ran = total - skipped
        successful = sum(1 for r in self.results if r.get("success"))
        failed = ran - successful

        print(f"\n{'='*60}")
        print(f"  ÖZET")
        print(f"  Toplam  : {total}  (çalıştı: {ran}, atlandı: {skipped})")
        print(f"  {_color(f'Başarılı: {successful}', '92')}  {_color(f'Başarısız: {failed}', '91' if failed else '90')}")
        print(f"{'='*60}")

        if failed > 0:
            print("\n  Başarısız adımlar:")
            for r in self.results:
                if not r.get("success") and not r.get("skipped"):
                    sc = r.get("status_code", 0)
                    print(f"    • [{r['step_id']}]  HTTP {sc}")
                    if isinstance(r.get("response"), dict):
                        msg = r["response"].get("message") or r["response"].get("title", "")
                        if msg:
                            print(f"       {msg}")

    def _save_results(self) -> None:
        data = {
            "final_state": self.state,
            "results": self.results,
        }
        with open(RESULTS_FILE, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        print(f"\n  Sonuçlar kaydedildi: {RESULTS_FILE}")

    def list_steps(self) -> None:
        print(f"\nFlow adımları ({len(self.flow)} adet):")
        for step in self.flow:
            sid = step.get("id", "?")
            method = step.get("method", "GET").upper()
            path = step.get("path", "")
            desc = step.get("description", "")
            print(f"  {sid:<30}  {method:<7} {path:<40}  {desc}")


# ─────────────────────────────────────────────────────────────────────────────
# CLI
# ─────────────────────────────────────────────────────────────────────────────

def main() -> None:
    parser = argparse.ArgumentParser(
        description="Exodus Request Automation — generated_payloads.json kullanarak API'ye istek atar"
    )
    parser.add_argument("--step", metavar="ID", help="Sadece bu adımı çalıştır")
    parser.add_argument("--from", dest="from_step", metavar="ID", help="Bu adımdan itibaren çalıştır")
    parser.add_argument("--list", action="store_true", help="Tüm adımları listele")
    parser.add_argument("--delay", type=float, default=0.4, help="Adımlar arası bekleme (saniye, default: 0.4)")
    parser.add_argument("--no-delay", action="store_true", help="Adımlar arası bekleme olmasın")
    args = parser.parse_args()

    delay = 0.0 if args.no_delay else args.delay
    automation = ExodusAutomation(delay=delay)

    if args.list:
        automation.list_steps()
        return

    if args.step:
        automation.run_single(args.step)
    elif args.from_step:
        automation.run_from(args.from_step)
    else:
        automation.run_all()


if __name__ == "__main__":
    main()
