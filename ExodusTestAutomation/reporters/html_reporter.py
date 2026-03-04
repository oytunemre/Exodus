"""
Tarayıcıda açılan güzel HTML raporu üretir.
Bootstrap 5 + highlight.js + jsondiffpatch.js kullanır.
"""
import json
import os
import webbrowser
from datetime import datetime
from typing import List, Dict, Any, Optional


REPORTS_DIR = os.path.join(os.path.dirname(os.path.dirname(__file__)), "reports")


def generate(
    endpoint_results: List[Dict[str, Any]],
    scenario_results: Optional[List[Dict[str, Any]]] = None,
    open_browser: bool = True,
) -> str:
    """HTML raporu oluştur, dosyaya yaz ve opsiyonel olarak tarayıcıda aç."""
    os.makedirs(REPORTS_DIR, exist_ok=True)

    timestamp = datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
    filename = f"report_{timestamp}.html"
    filepath = os.path.join(REPORTS_DIR, filename)

    html = _build_html(endpoint_results, scenario_results or [], timestamp)

    with open(filepath, "w", encoding="utf-8") as f:
        f.write(html)

    if open_browser:
        webbrowser.open(f"file://{os.path.abspath(filepath)}")

    return filepath


def _build_html(
    results: List[Dict[str, Any]],
    scenario_results: List[Dict[str, Any]],
    timestamp: str,
) -> str:
    total = len(results)
    success = sum(1 for r in results if r.get("status_code") and 200 <= r["status_code"] < 300)
    client_errors = sum(1 for r in results if r.get("status_code") and 400 <= r["status_code"] < 500)
    server_errors = sum(1 for r in results if r.get("status_code") and r["status_code"] >= 500)
    errors = sum(1 for r in results if r.get("error"))
    times = [r["response_time_ms"] for r in results if r.get("response_time_ms")]
    avg_time = round(sum(times) / len(times), 1) if times else 0

    # Unique tags
    tags = sorted(set(r.get("tag", "Untagged") for r in results))

    endpoint_rows = "\n".join(_build_endpoint_row(r, i) for i, r in enumerate(results))
    scenario_sections = "\n".join(_build_scenario_section(s) for s in scenario_results)

    tag_options = "\n".join(
        f'<option value="{t}">{t}</option>' for t in tags
    )

    return f"""<!DOCTYPE html>
<html lang="tr">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Exodus API Test Raporu — {timestamp}</title>
<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">
<link href="https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/styles/github-dark.min.css" rel="stylesheet">
<style>
  body {{ background: #0d1117; color: #e6edf3; font-family: 'Segoe UI', sans-serif; }}
  .navbar {{ background: #161b22 !important; border-bottom: 1px solid #30363d; }}
  .card {{ background: #161b22; border: 1px solid #30363d; }}
  .stat-card {{ border-left: 4px solid; }}
  .stat-success {{ border-color: #3fb950; }}
  .stat-warning {{ border-color: #d29922; }}
  .stat-danger  {{ border-color: #f85149; }}
  .stat-info    {{ border-color: #58a6ff; }}
  table {{ color: #e6edf3; }}
  .table {{ --bs-table-bg: transparent; --bs-table-color: #e6edf3; }}
  .table thead th {{ background: #21262d; border-color: #30363d; position: sticky; top: 0; z-index: 1; }}
  .table tbody tr {{ border-color: #30363d; }}
  .table tbody tr:hover {{ background: #1c2128 !important; cursor: pointer; }}
  .badge-method {{ font-size: 0.7rem; font-weight: 700; padding: 3px 8px; border-radius: 4px; }}
  .m-get    {{ background: #0d4a6b; color: #79c0ff; }}
  .m-post   {{ background: #0a3d2e; color: #3fb950; }}
  .m-put    {{ background: #1f2d5a; color: #79c0ff; }}
  .m-patch  {{ background: #3d1f5a; color: #d2a8ff; }}
  .m-delete {{ background: #5a1f1f; color: #f85149; }}
  .status-2xx {{ color: #3fb950; font-weight: 600; }}
  .status-4xx {{ color: #d29922; font-weight: 600; }}
  .status-5xx {{ color: #f85149; font-weight: 600; }}
  .status-err {{ color: #f85149; font-weight: 600; }}
  .detail-panel {{ background: #0d1117; border: 1px solid #30363d; border-top: none; }}
  .detail-panel pre {{ max-height: 400px; overflow-y: auto; border-radius: 6px; }}
  .diff-added   {{ background: #0a3d2e; }}
  .diff-removed {{ background: #5a1f1f; }}
  .diff-changed {{ background: #2d2e00; }}
  .filter-bar {{ background: #161b22; border: 1px solid #30363d; padding: 12px 16px; border-radius: 8px; }}
  .scenario-step {{ border-left: 3px solid #30363d; padding: 8px 12px; margin: 4px 0; }}
  .step-success {{ border-color: #3fb950; }}
  .step-failed  {{ border-color: #f85149; }}
  .step-skipped {{ border-color: #d29922; }}
  .nav-tabs .nav-link {{ color: #8b949e; }}
  .nav-tabs .nav-link.active {{ color: #e6edf3; background: #21262d; border-color: #30363d; }}
  .hljs {{ background: #161b22 !important; padding: 1rem; }}
  .diff-table td {{ font-family: monospace; font-size: 0.82rem; vertical-align: top; padding: 4px 8px; }}
  .diff-table {{ width: 100%; border-collapse: collapse; }}
  .diff-table td:first-child {{ border-right: 1px solid #30363d; width: 50%; }}
  select, input {{ background: #21262d !important; color: #e6edf3 !important; border-color: #30363d !important; }}
  select option {{ background: #21262d; }}
</style>
</head>
<body>

<nav class="navbar navbar-dark px-4 py-2">
  <span class="navbar-brand fw-bold">
    <span class="text-primary">Exodus</span> API Test Raporu
  </span>
  <small class="text-muted">{timestamp.replace("_", " ")}</small>
</nav>

<div class="container-fluid mt-3 px-4">

  <!-- Stats Cards -->
  <div class="row g-3 mb-4">
    <div class="col-6 col-md-3 col-lg-2">
      <div class="card stat-card stat-info p-3">
        <div class="text-muted small">Toplam</div>
        <div class="fs-3 fw-bold">{total}</div>
      </div>
    </div>
    <div class="col-6 col-md-3 col-lg-2">
      <div class="card stat-card stat-success p-3">
        <div class="text-muted small">2xx Başarılı</div>
        <div class="fs-3 fw-bold text-success">{success}</div>
        <small class="text-muted">{round(success/total*100) if total else 0}%</small>
      </div>
    </div>
    <div class="col-6 col-md-3 col-lg-2">
      <div class="card stat-card stat-warning p-3">
        <div class="text-muted small">4xx İstemci</div>
        <div class="fs-3 fw-bold text-warning">{client_errors}</div>
      </div>
    </div>
    <div class="col-6 col-md-3 col-lg-2">
      <div class="card stat-card stat-danger p-3">
        <div class="text-muted small">5xx Sunucu</div>
        <div class="fs-3 fw-bold text-danger">{server_errors}</div>
      </div>
    </div>
    <div class="col-6 col-md-3 col-lg-2">
      <div class="card stat-card stat-danger p-3">
        <div class="text-muted small">Bağlantı Hatası</div>
        <div class="fs-3 fw-bold text-danger">{errors}</div>
      </div>
    </div>
    <div class="col-6 col-md-3 col-lg-2">
      <div class="card stat-card stat-info p-3">
        <div class="text-muted small">Ort. Süre</div>
        <div class="fs-3 fw-bold text-info">{avg_time}ms</div>
      </div>
    </div>
  </div>

  <!-- Tabs -->
  <ul class="nav nav-tabs mb-3" id="mainTabs">
    <li class="nav-item">
      <button class="nav-link active" data-bs-toggle="tab" data-bs-target="#endpointTab">
        Endpoint Testleri <span class="badge bg-secondary ms-1">{total}</span>
      </button>
    </li>
    <li class="nav-item">
      <button class="nav-link" data-bs-toggle="tab" data-bs-target="#scenarioTab">
        Senaryo Testleri <span class="badge bg-secondary ms-1">{len(scenario_results)}</span>
      </button>
    </li>
  </ul>

  <div class="tab-content">

    <!-- Endpoint Tab -->
    <div class="tab-pane fade show active" id="endpointTab">

      <!-- Filters -->
      <div class="filter-bar mb-3 d-flex flex-wrap gap-2 align-items-center">
        <select id="filterMethod" class="form-select form-select-sm" style="width:120px">
          <option value="">Tüm Metodlar</option>
          <option>GET</option><option>POST</option><option>PUT</option>
          <option>PATCH</option><option>DELETE</option>
        </select>
        <select id="filterStatus" class="form-select form-select-sm" style="width:140px">
          <option value="">Tüm Status</option>
          <option value="2xx">2xx Başarılı</option>
          <option value="4xx">4xx Hata</option>
          <option value="5xx">5xx Hata</option>
          <option value="err">Bağlantı Hatası</option>
        </select>
        <select id="filterTag" class="form-select form-select-sm" style="width:180px">
          <option value="">Tüm Controller'lar</option>
          {tag_options}
        </select>
        <select id="filterDiff" class="form-select form-select-sm" style="width:150px">
          <option value="">Tüm Sonuçlar</option>
          <option value="changed">Değişenler</option>
          <option value="unchanged">Değişmeyenler</option>
        </select>
        <input id="filterSearch" class="form-control form-select-sm" placeholder="Endpoint ara..." style="width:220px">
        <button class="btn btn-sm btn-outline-secondary" onclick="resetFilters()">Sıfırla</button>
        <span id="filteredCount" class="text-muted small ms-2"></span>
      </div>

      <!-- Table -->
      <div class="table-responsive">
        <table class="table table-sm" id="endpointTable">
          <thead>
            <tr>
              <th style="width:90px">Method</th>
              <th>Endpoint</th>
              <th style="width:70px">Status</th>
              <th style="width:80px">Süre</th>
              <th style="width:160px">Controller</th>
              <th style="width:60px">Diff</th>
            </tr>
          </thead>
          <tbody id="endpointBody">
            {endpoint_rows}
          </tbody>
        </table>
      </div>
    </div>

    <!-- Scenario Tab -->
    <div class="tab-pane fade" id="scenarioTab">
      {scenario_sections if scenario_sections else '<div class="text-muted p-3">Henüz senaryo çalıştırılmadı. <code>python main.py scenario all</code> komutunu çalıştırın.</div>'}
    </div>

  </div><!-- tab-content -->
</div><!-- container -->

<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
<script src="https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/highlight.min.js"></script>
<script>
// Highlight.js
hljs.highlightAll();

// Satıra tıklayınca detail panel aç/kapat
function toggleDetail(idx) {{
  const row = document.getElementById('detail-' + idx);
  if (row) {{
    row.style.display = row.style.display === 'none' ? 'table-row' : 'none';
    // highlight.js yeniden uygula
    row.querySelectorAll('pre code').forEach(el => hljs.highlightElement(el));
  }}
}}

// Filtre
function applyFilters() {{
  const method = document.getElementById('filterMethod').value.toUpperCase();
  const status = document.getElementById('filterStatus').value;
  const tag = document.getElementById('filterTag').value.toLowerCase();
  const diff = document.getElementById('filterDiff').value;
  const search = document.getElementById('filterSearch').value.toLowerCase();

  const rows = document.querySelectorAll('#endpointBody tr.endpoint-row');
  let visible = 0;

  rows.forEach(row => {{
    const rowMethod = row.dataset.method || '';
    const rowStatus = parseInt(row.dataset.status) || 0;
    const rowTag = (row.dataset.tag || '').toLowerCase();
    const rowDiff = row.dataset.diff || 'unchanged';
    const rowPath = (row.dataset.path || '').toLowerCase();

    let show = true;
    if (method && rowMethod !== method) show = false;
    if (status === '2xx' && !(rowStatus >= 200 && rowStatus < 300)) show = false;
    if (status === '4xx' && !(rowStatus >= 400 && rowStatus < 500)) show = false;
    if (status === '5xx' && !(rowStatus >= 500)) show = false;
    if (status === 'err' && rowStatus !== 0) show = false;
    if (tag && rowTag !== tag) show = false;
    if (diff === 'changed' && rowDiff !== 'changed') show = false;
    if (diff === 'unchanged' && rowDiff === 'changed') show = false;
    if (search && !rowPath.includes(search)) show = false;

    row.style.display = show ? '' : 'none';
    if (show) visible++;

    // detail row gizle
    const detailRow = row.nextElementSibling;
    if (detailRow && detailRow.classList.contains('detail-row')) {{
      if (!show) detailRow.style.display = 'none';
    }}
  }});

  document.getElementById('filteredCount').textContent = `${{visible}} / ${{rows.length}} gösteriliyor`;
}}

function resetFilters() {{
  ['filterMethod','filterStatus','filterTag','filterDiff'].forEach(id => document.getElementById(id).value = '');
  document.getElementById('filterSearch').value = '';
  applyFilters();
}}

['filterMethod','filterStatus','filterTag','filterDiff'].forEach(id =>
  document.getElementById(id).addEventListener('change', applyFilters)
);
document.getElementById('filterSearch').addEventListener('input', applyFilters);

// Init count
applyFilters();
</script>
</body>
</html>"""


def _status_class(code: Optional[int]) -> str:
    if code is None:
        return "status-err"
    if 200 <= code < 300:
        return "status-2xx"
    if 400 <= code < 500:
        return "status-4xx"
    return "status-5xx"


def _method_class(method: str) -> str:
    return f"m-{method.lower()}"


def _json_str(obj: Any) -> str:
    if obj is None:
        return "null"
    try:
        return json.dumps(obj, ensure_ascii=False, indent=2)
    except Exception:
        return str(obj)


def _build_endpoint_row(result: Dict[str, Any], idx: int) -> str:
    method = result["method"]
    path = result["path"]
    code = result.get("status_code")
    time_ms = result.get("response_time_ms")
    tag = result.get("tag", "")
    has_diff = result.get("has_diff", False)
    is_new = result.get("is_new", False)
    error = result.get("error")

    code_display = str(code) if code else (error or "ERR")
    diff_display = "●" if has_diff else ("★" if is_new else "≈")
    diff_val = "changed" if has_diff else "unchanged"
    status_num = code or 0

    payload_str = _json_str(result.get("payload"))
    response_str = _json_str(result.get("response_body"))

    # Diff section
    diff_html = ""
    if has_diff and result.get("baseline"):
        baseline_str = _json_str(result["baseline"].get("response_body"))
        diff_html = f"""
        <div class="mt-3">
          <strong class="text-warning">Değişiklikler</strong>
          <table class="diff-table mt-2 border border-secondary rounded">
            <thead>
              <tr>
                <td class="text-muted p-2 border-bottom border-secondary">
                  <strong>Baseline</strong> (status: {result['baseline'].get('status_code', '?')})
                </td>
                <td class="text-muted p-2 border-bottom border-secondary">
                  <strong>Current</strong> (status: {code})
                </td>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td><pre><code class="language-json">{baseline_str.replace('<','&lt;').replace('>','&gt;')}</code></pre></td>
                <td><pre><code class="language-json">{response_str.replace('<','&lt;').replace('>','&gt;')}</code></pre></td>
              </tr>
            </tbody>
          </table>
        </div>"""

    detail_row = f"""
<tr class="detail-row" id="detail-{idx}" style="display:none">
  <td colspan="6" class="detail-panel p-3">
    <div class="row g-3">
      <div class="col-md-6">
        <strong class="text-info">Request</strong>
        <div class="text-muted small mt-1">{method} {result.get('resolved_path', path)}</div>
        <pre class="mt-2"><code class="language-json">{payload_str.replace('<','&lt;').replace('>','&gt;')}</code></pre>
      </div>
      <div class="col-md-6">
        <strong class="text-info">Response</strong>
        <div class="mt-1">
          <span class="{_status_class(code)}">{code_display}</span>
          <span class="text-muted small ms-2">{time_ms}ms</span>
        </div>
        <pre class="mt-2"><code class="language-json">{response_str.replace('<','&lt;').replace('>','&gt;')}</code></pre>
      </div>
    </div>
    {diff_html}
  </td>
</tr>"""

    return f"""
<tr class="endpoint-row"
    data-method="{method}"
    data-status="{status_num}"
    data-tag="{tag}"
    data-path="{path}"
    data-diff="{diff_val}"
    onclick="toggleDetail({idx})">
  <td><span class="badge-method {_method_class(method)}">{method}</span></td>
  <td><code class="text-light" style="font-size:0.82rem">{path}</code></td>
  <td><span class="{_status_class(code)}">{code_display}</span></td>
  <td class="text-muted small">{time_ms}ms</td>
  <td class="text-muted small">{tag}</td>
  <td class="text-center">
    <span class="{'text-warning' if has_diff else ('text-info' if is_new else 'text-muted')}">
      {diff_display}
    </span>
  </td>
</tr>
{detail_row}"""


def _build_scenario_section(scenario: Dict[str, Any]) -> str:
    name = scenario.get("name", "Senaryo")
    steps = scenario.get("steps", [])
    total = len(steps)
    success = sum(1 for s in steps if s.get("success"))
    skipped = sum(1 for s in steps if s.get("skipped"))
    failed = total - success - skipped

    steps_html = "\n".join(_build_step_html(s, i) for i, s in enumerate(steps))

    return f"""
<div class="card mb-4">
  <div class="card-header d-flex justify-content-between align-items-center">
    <strong>{name}</strong>
    <div>
      <span class="badge bg-success ms-1">{success} başarılı</span>
      <span class="badge bg-warning ms-1">{skipped} atlandı</span>
      <span class="badge bg-danger ms-1">{failed} başarısız</span>
    </div>
  </div>
  <div class="card-body p-2">
    {steps_html}
  </div>
</div>"""


def _build_step_html(step: Dict[str, Any], idx: int) -> str:
    name = step.get("name", f"Adım {idx+1}")
    success = step.get("success", False)
    skipped = step.get("skipped", False)
    code = step.get("status_code")
    error = step.get("error")
    time_ms = step.get("response_time_ms", "")

    if skipped:
        icon = "⏭️"
        css_class = "step-skipped"
        status_text = "Atlandı"
    elif success:
        icon = "✅"
        css_class = "step-success"
        status_text = str(code) if code else "OK"
    else:
        icon = "❌"
        css_class = "step-failed"
        status_text = str(code) if code else (error or "ERR")

    return f"""
<div class="scenario-step {css_class}">
  {icon} <strong>{name}</strong>
  <span class="text-muted small ms-2">{status_text}</span>
  {f'<span class="text-muted small ms-1">({time_ms}ms)</span>' if time_ms else ''}
</div>"""


from typing import Optional
