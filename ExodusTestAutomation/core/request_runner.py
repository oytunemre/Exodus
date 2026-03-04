"""
HTTP isteklerini çalıştırır, yanıtları toplar ve session state'i günceller.
"""
import time
import httpx
from typing import Dict, Any, Optional, List
import config
from core import session_state
from core.payload_factory import PayloadFactory
from core.auth_manager import AuthManager

# Endpoint'lerin hangi role'ü gerektirdiğini belirle
ROLE_HINTS = {
    "/api/admin/": "admin",
    "/api/seller/": "seller",
    "/api/products": "seller",
    "/api/listings": "seller",
    "/api/campaigns": "seller",
    "/api/cart": "customer",
    "/api/orders": "customer",
    "/api/addresses": "customer",
    "/api/reviews": "customer",
    "/api/wishlist": "customer",
    "/api/notifications": "customer",
    "/api/auth/": None,
}

# Bağımlılık zinciri: bu endpoint'ler önce çalışmalı
PRIORITY_ORDER = [
    ("POST", "/api/auth/register"),
    ("POST", "/api/auth/login"),
    ("POST", "/api/admin/categories"),
    ("POST", "/api/admin/brands"),
    ("POST", "/api/products"),
    ("POST", "/api/listings"),
    ("POST", "/api/addresses"),
    ("POST", "/api/cart"),
    ("POST", "/api/orders"),
]


class RequestRunner:
    def __init__(self, base_url: str, auth_manager: AuthManager):
        self.base_url = base_url
        self.auth_manager = auth_manager
        self.payload_factory = PayloadFactory()
        self.client = httpx.Client(
            base_url=base_url,
            timeout=config.TIMEOUT,
            verify=False,
        )

    def _get_role(self, path: str) -> Optional[str]:
        """Path'e göre uygun role'ü belirle."""
        for prefix, role in ROLE_HINTS.items():
            if path.startswith(prefix):
                return role
        return None

    def _get_headers(self, path: str, requires_auth: bool) -> Dict[str, str]:
        """Request için header'ları hazırla."""
        headers = {"Content-Type": "application/json"}
        if requires_auth:
            role = self._get_role(path) or "admin"
            headers.update(self.auth_manager.get_headers(role))
        return headers

    def run_endpoint(self, endpoint: Dict) -> Dict[str, Any]:
        """Tek bir endpoint'e istek at, sonucu döndür."""
        method = endpoint["method"]
        path_template = endpoint["path"]
        requires_auth = endpoint.get("requires_auth", False)

        # Path parametrelerini doldur
        path = self.payload_factory.resolve_path_params(path_template)

        # Payload oluştur
        payload = self.payload_factory.build(endpoint)

        # Headers
        headers = self._get_headers(path_template, requires_auth)

        # İsteği at
        start_time = time.time()
        result = {
            "method": method,
            "path": path_template,
            "resolved_path": path,
            "tag": endpoint.get("tag", ""),
            "payload": payload,
            "headers_sent": {k: v for k, v in headers.items() if k != "Authorization"},
            "status_code": None,
            "response_body": None,
            "response_time_ms": None,
            "error": None,
        }

        try:
            kwargs = {"headers": headers}
            if payload and method in ("POST", "PUT", "PATCH"):
                kwargs["json"] = payload

            response = self.client.request(method, path, **kwargs)
            elapsed = (time.time() - start_time) * 1000

            result["status_code"] = response.status_code
            result["response_time_ms"] = round(elapsed, 1)

            try:
                result["response_body"] = response.json()
            except Exception:
                result["response_body"] = response.text

            # Hatalı response'ları logla
            if response.status_code >= 400:
                import json as _json
                body = result["response_body"]
                body_str = _json.dumps(body, ensure_ascii=False) if isinstance(body, dict) else str(body)
                print(f"  [WARN] {method} {path} → {response.status_code} | {body_str[:300]}")

            # Başarılı POST response'larından ID'leri session'a kaydet
            if response.status_code in (200, 201) and method == "POST":
                self._extract_ids(path_template, result["response_body"])

        except httpx.TimeoutException:
            result["error"] = "Timeout"
            result["response_time_ms"] = config.TIMEOUT * 1000
        except Exception as e:
            result["error"] = str(e)

        return result

    def _extract_ids(self, path: str, body: Any) -> None:
        """Başarılı response'dan ID'leri çıkar ve session'a kaydet."""
        if not isinstance(body, dict):
            return

        # ID'yi farklı field isimlerinden bul
        def find_id(obj):
            for key in ("id", "Id", "ID"):
                if key in obj:
                    return obj[key]
            if "data" in obj and isinstance(obj["data"], dict):
                for key in ("id", "Id", "ID"):
                    if key in obj["data"]:
                        return obj["data"][key]
            return None

        entity_id = find_id(body)
        if entity_id is None:
            return

        path_lower = path.lower()
        if "/categories" in path_lower:
            session_state.set("created_category_id", entity_id)
        elif "/brands" in path_lower:
            session_state.set("created_brand_id", entity_id)
        elif "/products" in path_lower and "/listings" not in path_lower:
            session_state.set("created_product_id", entity_id)
        elif "/listings" in path_lower:
            session_state.set("created_listing_id", entity_id)
        elif "/cart" in path_lower and "/items" in path_lower:
            session_state.set("created_cart_item_id", entity_id)
        elif "/cart" in path_lower:
            session_state.set("created_cart_id", entity_id)
        elif "/orders" in path_lower:
            session_state.set("created_order_id", entity_id)
        elif "/address" in path_lower:  # /api/Address ve /api/.../addresses her ikisini de yakala
            session_state.set("created_address_id", entity_id)
        elif "/reviews" in path_lower:
            session_state.set("created_review_id", entity_id)
        elif "/campaigns" in path_lower:
            session_state.set("created_campaign_id", entity_id)
        elif "/coupons" in path_lower:
            session_state.set("created_coupon_id", entity_id)
        elif "/wishlist" in path_lower:
            session_state.set("created_wishlist_item_id", entity_id)

    def run_all(self, endpoints: List[Dict], group: Optional[str] = None) -> List[Dict]:
        """Tüm endpoint'leri sırayla çalıştır."""
        # Group filtresi
        if group:
            endpoints = [e for e in endpoints if e.get("tag", "").lower() == group.lower()]

        # Önce öncelikli endpoint'leri çalıştır
        priority_paths = [p for _, p in PRIORITY_ORDER]
        priority = []
        rest = []

        for ep in endpoints:
            is_priority = any(
                ep["method"] == m and ep["path"].startswith(p)
                for m, p in PRIORITY_ORDER
            )
            if is_priority:
                priority.append(ep)
            else:
                rest.append(ep)

        ordered = priority + rest
        results = []

        for ep in ordered:
            result = self.run_endpoint(ep)
            results.append(result)

        return results

    def close(self):
        self.client.close()
