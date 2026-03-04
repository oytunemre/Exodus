"""
İki test çalıştırmasını karşılaştırır.
DeepDiff ile farkları hesaplar.
"""
from typing import Dict, Any, Optional, List


def _safe_deepdiff(baseline: Any, current: Any) -> Dict:
    """DeepDiff ile karşılaştır, kütüphane yoksa basit karşılaştır."""
    try:
        from deepdiff import DeepDiff
        diff = DeepDiff(baseline, current, ignore_order=True)
        return diff.to_dict() if diff else {}
    except ImportError:
        # DeepDiff yoksa basit karşılaştır
        if baseline == current:
            return {}
        return {"simple_diff": {"baseline": baseline, "current": current}}


def compare_results(
    baseline_snapshots: Dict[str, Dict],
    current_results: List[Dict],
) -> List[Dict]:
    """
    Her endpoint için baseline ile current'ı karşılaştır.
    Current results'a diff bilgisi ekle ve döndür.
    """
    from core.snapshot_manager import _path_to_filename

    enriched = []
    for result in current_results:
        filename = _path_to_filename(result["method"], result["path"])
        baseline = baseline_snapshots.get(filename)

        if baseline:
            status_changed = baseline.get("status_code") != result.get("status_code")
            body_diff = _safe_deepdiff(
                baseline.get("response_body"),
                result.get("response_body"),
            )
            has_diff = status_changed or bool(body_diff)

            result["baseline"] = baseline
            result["diff"] = body_diff
            result["has_diff"] = has_diff
            result["status_changed"] = status_changed
        else:
            result["baseline"] = None
            result["diff"] = None
            result["has_diff"] = False
            result["status_changed"] = False
            result["is_new"] = True

        enriched.append(result)

    return enriched
