"""
Test sonuçlarını JSON dosyası olarak kaydeder ve yükler.
"""
import json
import os
import re
from datetime import datetime
from typing import Dict, Any, Optional, List


SNAPSHOTS_DIR = os.path.join(os.path.dirname(os.path.dirname(__file__)), "snapshots")
BASELINE_DIR = os.path.join(SNAPSHOTS_DIR, "baseline")


def _path_to_filename(method: str, path: str) -> str:
    """Endpoint'i dosya adına dönüştür. Örn: GET /api/products → GET__api_products.json"""
    safe_path = re.sub(r"[/{}\s]", "_", path).strip("_")
    safe_path = re.sub(r"_+", "_", safe_path)
    return f"{method}__{safe_path}.json"


def save_baseline(results: List[Dict[str, Any]]) -> None:
    """İlk çalıştırma snapshot'larını baseline olarak kaydet."""
    os.makedirs(BASELINE_DIR, exist_ok=True)
    for result in results:
        filename = _path_to_filename(result["method"], result["path"])
        filepath = os.path.join(BASELINE_DIR, filename)
        with open(filepath, "w", encoding="utf-8") as f:
            json.dump(result, f, ensure_ascii=False, indent=2)


def save_run(results: List[Dict[str, Any]]) -> str:
    """Çalıştırma sonuçlarını timestamp'li klasöre kaydet."""
    timestamp = datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
    run_dir = os.path.join(SNAPSHOTS_DIR, timestamp)
    os.makedirs(run_dir, exist_ok=True)

    for result in results:
        filename = _path_to_filename(result["method"], result["path"])
        filepath = os.path.join(run_dir, filename)
        with open(filepath, "w", encoding="utf-8") as f:
            json.dump(result, f, ensure_ascii=False, indent=2)

    return timestamp


def load_baseline() -> Optional[Dict[str, Dict]]:
    """Baseline snapshot'larını yükle. {filename: result} dict döner."""
    if not os.path.exists(BASELINE_DIR):
        return None

    snapshots = {}
    for filename in os.listdir(BASELINE_DIR):
        if filename.endswith(".json"):
            filepath = os.path.join(BASELINE_DIR, filename)
            with open(filepath, "r", encoding="utf-8") as f:
                snapshots[filename] = json.load(f)

    return snapshots if snapshots else None


def load_run(timestamp: str) -> Optional[Dict[str, Dict]]:
    """Belirli bir çalıştırmanın snapshot'larını yükle."""
    run_dir = os.path.join(SNAPSHOTS_DIR, timestamp)
    if not os.path.exists(run_dir):
        return None

    snapshots = {}
    for filename in os.listdir(run_dir):
        if filename.endswith(".json"):
            filepath = os.path.join(run_dir, filename)
            with open(filepath, "r", encoding="utf-8") as f:
                snapshots[filename] = json.load(f)

    return snapshots


def get_latest_run() -> Optional[str]:
    """En son çalıştırmanın timestamp'ini döndür."""
    if not os.path.exists(SNAPSHOTS_DIR):
        return None

    runs = [
        d for d in os.listdir(SNAPSHOTS_DIR)
        if os.path.isdir(os.path.join(SNAPSHOTS_DIR, d)) and d != "baseline"
    ]

    if not runs:
        return None

    return sorted(runs)[-1]


def has_baseline() -> bool:
    return os.path.exists(BASELINE_DIR) and bool(os.listdir(BASELINE_DIR))


def clear_baseline() -> None:
    """Baseline snapshot'larını temizle."""
    if os.path.exists(BASELINE_DIR):
        for f in os.listdir(BASELINE_DIR):
            os.remove(os.path.join(BASELINE_DIR, f))
