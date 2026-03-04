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
                token = None
                for key in ("token", "accessToken", "access_token", "jwt"):
                    if key in data:
                        token = data[key]
                        break
                # Nested data objesi
                if token is None and "data" in data and isinstance(data["data"], dict):
                    for key in ("token", "accessToken", "access_token", "jwt"):
                        if key in data["data"]:
                            token = data["data"][key]
                            break

                if token is None:
                    print(f"  [AUTH] Token field bulunamadı. Response: {data}")
                    return None

                # Kullanıcı ID'lerini session'a kaydet
                self._extract_user_ids(persona, data)
                return token
            else:
                try:
                    body = response.json()
                except Exception:
                    body = response.text
                print(f"  [AUTH] Login başarısız ({persona['email']}): HTTP {response.status_code} → {body}")
        except Exception as e:
            print(f"  [AUTH] Login hatası ({persona['email']}): {e}")
        return None

    def _extract_user_ids(self, persona: dict, data: dict) -> None:
        """Login response'dan user/seller ID'lerini session'a kaydet."""
        role = persona.get("role", "").lower()

        # ID'yi bul (data.id veya id)
        user_id = None
        seller_id = None

        src = data.get("data", data)
        if isinstance(src, dict):
            user_id = src.get("id") or src.get("userId") or src.get("user", {}).get("id")
            seller_id = src.get("sellerId") or src.get("sellerProfileId")

        if role == "admin" and user_id:
            session_state.set("admin_id", user_id)
        elif role == "seller":
            if user_id:
                session_state.set("seller_user_id", user_id)
            if seller_id:
                session_state.set("seller_id", seller_id)
        elif role == "customer" and user_id:
            session_state.set("customer_id", user_id)

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
