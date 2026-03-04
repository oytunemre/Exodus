"""
JWT token yönetimi.
Strateji: önce register dene, başarısız olursa (hesap zaten var) login yap.
"""
import httpx
from typing import Optional
import config
from core import session_state
from core.test_data import ADMIN, SELLER, CUSTOMER


class AuthManager:
    def __init__(self, base_url: str):
        self.base_url = base_url
        self.client = httpx.Client(
            base_url=base_url,
            timeout=config.TIMEOUT,
            verify=False,
        )

    def _try_register(self, persona: dict) -> None:
        """Register dene; hata olursa (zaten kayıtlı vs.) sessizce geç."""
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
            )
        except Exception:
            pass  # Register başarısız olsa da login deneriz

    def _login(self, persona: dict) -> Optional[str]:
        """Login yap ve token döndür."""
        try:
            response = self.client.post(
                "/api/auth/login",
                json={
                    "emailOrUsername": persona["email"],
                    "password": persona["password"],
                },
            )
            if response.status_code == 200:
                data = response.json()
                # Token farklı field adlarında olabilir
                for key in ("token", "accessToken", "access_token", "jwt"):
                    if key in data:
                        return data[key]
                # Nested data objesi
                if "data" in data and isinstance(data["data"], dict):
                    for key in ("token", "accessToken", "access_token", "jwt"):
                        if key in data["data"]:
                            return data["data"][key]
                print(f"  [AUTH] Token field bulunamadı. Response: {data}")
            else:
                try:
                    body = response.json()
                except Exception:
                    body = response.text
                print(f"  [AUTH] Login başarısız ({persona['email']}): HTTP {response.status_code} → {body}")
        except Exception as e:
            print(f"  [AUTH] Login hatası ({persona['email']}): {e}")
        return None

    def get_token(self, persona: dict) -> Optional[str]:
        """Direkt login yap (kullanıcılar DB'de zaten kayıtlı)."""
        return self._login(persona)

    def authenticate_all(self) -> dict:
        """Admin, seller ve customer token'larını al, session'a kaydet."""
        results = {}

        for name, persona, token_key in [
            ("admin", ADMIN, "admin_token"),
            ("seller", SELLER, "seller_token"),
            ("customer", CUSTOMER, "customer_token"),
        ]:
            token = self.get_token(persona)
            session_state.set(token_key, token)
            results[name] = token is not None
            status = "OK" if token else "FAIL"
            print(f"  [AUTH] {name}: {status}")

        return results

    def get_headers(self, role: str = "admin") -> dict:
        """Role'e göre Authorization header döndür."""
        token_key = f"{role}_token"
        token = session_state.get(token_key)
        if token:
            return {"Authorization": f"Bearer {token}"}
        return {}

    def close(self):
        self.client.close()
