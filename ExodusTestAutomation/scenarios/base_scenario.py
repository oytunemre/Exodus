"""
Tüm senaryolar için ortak temel sınıf.
Adım loglama, login, HTTP request ve assert metodları sağlar.
"""
import time
import httpx
from typing import Any, Dict, List, Optional
import config
from core import session_state
from core.auth_manager import AuthManager


class BaseScenario:
    def __init__(self, name: str):
        self.name = name
        self.steps: List[Dict[str, Any]] = []
        self.auth_manager = AuthManager(config.BASE_URL)
        self.client = httpx.Client(
            base_url=config.BASE_URL,
            timeout=config.TIMEOUT,
            verify=False,
        )
        self._failed_steps: set = set()

    def _request(
        self,
        method: str,
        path: str,
        json: Optional[Dict] = None,
        token: Optional[str] = None,
        params: Optional[Dict] = None,
    ) -> httpx.Response:
        headers = {"Content-Type": "application/json"}
        if token:
            headers["Authorization"] = f"Bearer {token}"

        return self.client.request(
            method,
            path,
            json=json,
            headers=headers,
            params=params,
        )

    def step(
        self,
        name: str,
        method: str,
        path: str,
        json: Optional[Dict] = None,
        token: Optional[str] = None,
        params: Optional[Dict] = None,
        depends_on: Optional[List[str]] = None,
        expected_status: int = 200,
        extract: Optional[Dict[str, str]] = None,
    ) -> Optional[Dict[str, Any]]:
        """
        Bir senaryo adımı çalıştır.

        Args:
            name: Adım adı (log ve rapor için)
            method: HTTP method
            path: Endpoint path
            json: Request body
            token: JWT token (opsiyonel)
            params: Query parametreleri
            depends_on: Bu adım çalışmadan önce başarılı olması gereken adımlar
            expected_status: Başarı için beklenen status kodu
            extract: Response'dan session'a kaydedilecek değerler
                     {'session_key': 'response.field.path'}
        """
        # Bağımlı adım başarısız mı?
        if depends_on:
            failed_deps = [d for d in depends_on if d in self._failed_steps]
            if failed_deps:
                step_result = {
                    "name": name,
                    "method": method,
                    "path": path,
                    "success": False,
                    "skipped": True,
                    "skip_reason": f"Bağımlı adım başarısız: {', '.join(failed_deps)}",
                    "status_code": None,
                    "response_body": None,
                    "response_time_ms": None,
                    "error": None,
                }
                self.steps.append(step_result)
                self._failed_steps.add(name)
                return None

        # İstek at
        start = time.time()
        step_result = {
            "name": name,
            "method": method,
            "path": path,
            "success": False,
            "skipped": False,
            "skip_reason": None,
            "status_code": None,
            "response_body": None,
            "response_time_ms": None,
            "error": None,
        }

        try:
            response = self._request(method, path, json=json, token=token, params=params)
            elapsed = round((time.time() - start) * 1000, 1)

            step_result["status_code"] = response.status_code
            step_result["response_time_ms"] = elapsed

            try:
                body = response.json()
            except Exception:
                body = response.text

            step_result["response_body"] = body

            # Başarı kontrolü
            if response.status_code == expected_status or (
                200 <= response.status_code < 300 and expected_status == 200
            ):
                step_result["success"] = True

                # ID'leri session'a kaydet
                if extract and isinstance(body, dict):
                    for session_key, response_path in extract.items():
                        value = body
                        for part in response_path.split("."):
                            if isinstance(value, dict):
                                value = value.get(part)
                            else:
                                value = None
                                break
                        if value is not None:
                            session_state.set(session_key, value)
            else:
                self._failed_steps.add(name)

        except httpx.TimeoutException:
            step_result["error"] = "Timeout"
            self._failed_steps.add(name)
        except Exception as e:
            step_result["error"] = str(e)
            self._failed_steps.add(name)

        self.steps.append(step_result)
        return step_result if step_result["success"] else None

    def login(self, email: str, password: str) -> Optional[str]:
        """Login yap ve token döndür."""
        try:
            response = self.client.post(
                "/api/auth/login",
                json={"emailOrUsername": email, "password": password},
                headers={"Content-Type": "application/json"},
            )
            if response.status_code == 200:
                body = response.json()
                for key in ("token", "accessToken", "access_token", "jwt"):
                    if key in body:
                        return body[key]
                    if "data" in body and isinstance(body["data"], dict) and key in body["data"]:
                        return body["data"][key]
        except Exception:
            pass
        return None

    def register_and_login(self, persona: dict) -> Optional[str]:
        """Register (varsa atla) + login."""
        try:
            self.client.post(
                "/api/auth/register",
                json={
                    "email": persona["email"],
                    "password": persona["password"],
                    "firstName": persona.get("firstName", "Test"),
                    "lastName": persona.get("lastName", "User"),
                    "role": persona.get("role", "Customer"),
                    "phoneNumber": persona.get("phoneNumber", "+905550000000"),
                },
                headers={"Content-Type": "application/json"},
            )
        except Exception:
            pass

        return self.login(persona["email"], persona["password"])

    def run(self) -> Dict[str, Any]:
        """Senaryoyu çalıştır. Alt sınıflar bu metodu override eder."""
        raise NotImplementedError

    def get_result(self) -> Dict[str, Any]:
        total = len(self.steps)
        success = sum(1 for s in self.steps if s["success"])
        skipped = sum(1 for s in self.steps if s["skipped"])
        failed = total - success - skipped

        return {
            "name": self.name,
            "steps": self.steps,
            "total": total,
            "success": success,
            "skipped": skipped,
            "failed": failed,
        }

    def close(self):
        self.client.close()
        self.auth_manager.close()
