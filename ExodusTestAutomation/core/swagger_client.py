"""
Swagger JSON'dan tüm endpoint'leri parse eder.
"""
import httpx
from typing import List, Dict, Any


class SwaggerClient:
    def __init__(self, swagger_url: str):
        self.swagger_url = swagger_url
        self._spec: Dict[str, Any] = {}

    def fetch(self) -> Dict[str, Any]:
        response = httpx.get(self.swagger_url, timeout=10, verify=False)
        response.raise_for_status()
        self._spec = response.json()
        return self._spec

    def get_endpoints(self) -> List[Dict[str, Any]]:
        """Swagger spec'inden tüm endpoint'leri düz liste olarak döner."""
        if not self._spec:
            self.fetch()

        endpoints = []
        paths = self._spec.get("paths", {})
        components = self._spec.get("components", {})

        for path, path_item in paths.items():
            for method, operation in path_item.items():
                if method.upper() not in ("GET", "POST", "PUT", "DELETE", "PATCH"):
                    continue

                tags = operation.get("tags", ["Untagged"])
                parameters = operation.get("parameters", [])
                request_body = operation.get("requestBody", None)

                # requestBody schema'sını çöz
                body_schema = None
                if request_body:
                    content = request_body.get("content", {})
                    json_content = content.get("application/json", content.get("multipart/form-data", {}))
                    body_schema = json_content.get("schema")
                    if body_schema:
                        body_schema = self._resolve_ref(body_schema, components)

                # Security gerektiriyor mu?
                security = operation.get("security", self._spec.get("security", []))
                requires_auth = bool(security)

                endpoints.append({
                    "method": method.upper(),
                    "path": path,
                    "tag": tags[0] if tags else "Untagged",
                    "operation_id": operation.get("operationId", ""),
                    "summary": operation.get("summary", ""),
                    "parameters": parameters,
                    "body_schema": body_schema,
                    "requires_auth": requires_auth,
                    "security": security,
                })

        return endpoints

    def _resolve_ref(self, schema: Dict, components: Dict) -> Dict:
        """$ref referanslarını çözümler."""
        if not isinstance(schema, dict):
            return schema

        if "$ref" in schema:
            ref_path = schema["$ref"].lstrip("#/").split("/")
            resolved = components
            for part in ref_path[1:]:  # components/ atla
                resolved = resolved.get(part, {})
            return self._resolve_ref(resolved, components)

        # allOf, anyOf, oneOf
        for combiner in ("allOf", "anyOf", "oneOf"):
            if combiner in schema:
                merged = {"type": "object", "properties": {}}
                for sub in schema[combiner]:
                    resolved = self._resolve_ref(sub, components)
                    if "properties" in resolved:
                        merged["properties"].update(resolved["properties"])
                return merged

        # Properties içindeki $ref'leri de çöz
        if "properties" in schema:
            resolved_props = {}
            for prop_name, prop_schema in schema["properties"].items():
                resolved_props[prop_name] = self._resolve_ref(prop_schema, components)
            return {**schema, "properties": resolved_props}

        return schema
