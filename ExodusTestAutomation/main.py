"""
Exodus API Test Automation — CLI giriş noktası
"""
import sys
import os

# Proje kökünü Python path'e ekle
sys.path.insert(0, os.path.dirname(__file__))

import click
import config
from core.swagger_client import SwaggerClient
from core.auth_manager import AuthManager
from core.request_runner import RequestRunner
from core import snapshot_manager, session_state
from core.comparator import compare_results
from reporters import terminal_reporter, html_reporter


@click.group()
def cli():
    """Exodus API Test Automation Tool"""
    pass


# ── ENDPOINT TESTLERİ ───────────────────────────────────────────────────────

@cli.command()
@click.option("--group", default=None, help="Sadece belirli bir controller'ı test et (örn: Auth, Products)")
@click.option("--clean", is_flag=True, help="Çalıştırmadan önce test verilerini temizle")
@click.option("--no-browser", is_flag=True, help="HTML raporu otomatik açma")
def run(group, clean, no_browser):
    """Tüm endpoint'leri test et ve snapshot kaydet."""
    if clean:
        click.echo("Test verileri temizleniyor...")
        _do_cleanup()

    click.echo(f"Swagger alınıyor: {config.SWAGGER_URL}")
    client = SwaggerClient(config.SWAGGER_URL)
    try:
        endpoints = client.get_endpoints()
    except Exception as e:
        click.echo(f"Swagger alınamadı: {e}", err=True)
        click.echo("API çalışıyor mu? dotnet run ile başlatın.", err=True)
        sys.exit(1)

    click.echo(f"{len(endpoints)} endpoint bulundu")

    # Auth
    click.echo("Token alınıyor...")
    auth = AuthManager(config.BASE_URL)
    auth.authenticate_all()

    # Test
    runner = RequestRunner(config.BASE_URL, auth)
    click.echo(f"Testler çalıştırılıyor{f' (grup: {group})' if group else ''}...")
    results = runner.run_all(endpoints, group=group)

    # Snapshot kaydet
    if snapshot_manager.has_baseline():
        timestamp = snapshot_manager.save_run(results)
        click.echo(f"Snapshot kaydedildi: {timestamp}")
    else:
        snapshot_manager.save_baseline(results)
        click.echo("Baseline snapshot oluşturuldu.")

    # Terminal raporu
    terminal_reporter.print_run_summary(results, "Endpoint Test Sonuçları")

    # HTML raporu
    report_path = html_reporter.generate(results, open_browser=not no_browser)
    click.echo(f"HTML rapor: {report_path}")

    runner.close()
    auth.close()


@cli.command()
@click.option("--no-browser", is_flag=True, help="HTML raporu otomatik açma")
def compare(no_browser):
    """Son çalıştırmayla baseline'ı karşılaştır."""
    baseline = snapshot_manager.load_baseline()
    if not baseline:
        click.echo("Baseline yok. Önce 'python main.py run' çalıştırın.", err=True)
        sys.exit(1)

    click.echo(f"Swagger alınıyor: {config.SWAGGER_URL}")
    client = SwaggerClient(config.SWAGGER_URL)
    try:
        endpoints = client.get_endpoints()
    except Exception as e:
        click.echo(f"Swagger alınamadı: {e}", err=True)
        sys.exit(1)

    auth = AuthManager(config.BASE_URL)
    auth.authenticate_all()

    runner = RequestRunner(config.BASE_URL, auth)
    click.echo("Testler çalıştırılıyor...")
    results = runner.run_all(endpoints)

    # Karşılaştır
    results = compare_results(baseline, results)

    # Snapshot kaydet
    timestamp = snapshot_manager.save_run(results)
    click.echo(f"Snapshot kaydedildi: {timestamp}")

    # Terminal raporu
    terminal_reporter.print_run_summary(results, "Karşılaştırma Sonuçları")

    # HTML raporu
    report_path = html_reporter.generate(results, open_browser=not no_browser)
    click.echo(f"HTML rapor: {report_path}")

    runner.close()
    auth.close()


@cli.command(name="list")
def list_endpoints():
    """Endpoint'leri listele, istek atma."""
    click.echo(f"Swagger alınıyor: {config.SWAGGER_URL}")
    client = SwaggerClient(config.SWAGGER_URL)
    try:
        endpoints = client.get_endpoints()
    except Exception as e:
        click.echo(f"Swagger alınamadı: {e}", err=True)
        sys.exit(1)

    # Gruplara ayır
    from collections import defaultdict
    grouped = defaultdict(list)
    for ep in endpoints:
        grouped[ep["tag"]].append(ep)

    total = 0
    for tag in sorted(grouped.keys()):
        eps = grouped[tag]
        click.echo(f"\n[{tag}] ({len(eps)} endpoint)")
        for ep in eps:
            auth_icon = "🔒" if ep["requires_auth"] else "  "
            click.echo(f"  {auth_icon} {ep['method']:6} {ep['path']}")
        total += len(eps)

    click.echo(f"\nToplam: {total} endpoint, {len(grouped)} controller")


@cli.command()
def report():
    """Son oluşturulan HTML raporu tarayıcıda aç."""
    import webbrowser
    import glob

    reports_dir = os.path.join(os.path.dirname(__file__), "reports")
    reports = sorted(glob.glob(os.path.join(reports_dir, "*.html")))

    if not reports:
        click.echo("Rapor bulunamadı. Önce 'python main.py run' çalıştırın.", err=True)
        sys.exit(1)

    latest = reports[-1]
    click.echo(f"Rapor açılıyor: {latest}")
    webbrowser.open(f"file://{os.path.abspath(latest)}")


# ── SENARYO TESTLERİ ────────────────────────────────────────────────────────

@cli.command()
@click.argument("scenario", type=click.Choice(["buyer", "seller", "admin", "all"]))
@click.option("--no-browser", is_flag=True, help="HTML raporu otomatik açma")
def scenario(scenario, no_browser):
    """Senaryo testleri çalıştır (endpoint testlerinden bağımsız)."""
    from scenarios.buyer_flow import BuyerFlow
    from scenarios.seller_flow import SellerFlow
    from scenarios.admin_flow import AdminFlow

    flows_to_run = []
    if scenario == "buyer":
        flows_to_run = [BuyerFlow()]
    elif scenario == "seller":
        flows_to_run = [SellerFlow()]
    elif scenario == "admin":
        flows_to_run = [AdminFlow()]
    elif scenario == "all":
        # Admin önce çalışmalı (category oluşturur)
        flows_to_run = [AdminFlow(), SellerFlow(), BuyerFlow()]

    click.echo(f"{len(flows_to_run)} senaryo çalıştırılıyor...")

    all_results = []
    for flow in flows_to_run:
        result = flow.run()
        all_results.append(result)
        terminal_reporter.print_scenario_summary(result["name"], result["steps"])
        flow.close()

    # Özet
    total_steps = sum(r["total"] for r in all_results)
    total_success = sum(r["success"] for r in all_results)
    total_failed = sum(r["failed"] for r in all_results)
    click.echo(f"\nGenel: {total_success}/{total_steps} adım başarılı, {total_failed} başarısız")

    # HTML rapor (senaryo sekmesine yaz)
    report_path = html_reporter.generate(
        endpoint_results=[],
        scenario_results=all_results,
        open_browser=not no_browser,
    )
    click.echo(f"HTML rapor: {report_path}")


# ── CLEANUP ─────────────────────────────────────────────────────────────────

@cli.command()
def cleanup():
    """Test verilerini temizle (DB'den sil)."""
    click.echo("Test verileri temizleniyor...")
    _do_cleanup()
    click.echo("Temizlik tamamlandı.")


def _do_cleanup():
    """Test verilerini API aracılığıyla sil."""
    import httpx

    auth = AuthManager(config.BASE_URL)
    auth.authenticate_all()

    admin_headers = auth.get_headers("admin")
    seller_headers = auth.get_headers("seller")

    client = httpx.Client(base_url=config.BASE_URL, timeout=config.TIMEOUT, verify=False)

    delete_operations = [
        # (headers, endpoint)
        (admin_headers, f"/api/admin/orders/{session_state.get('created_order_id')}"),
        (seller_headers, f"/api/listings/{session_state.get('created_listing_id')}"),
        (seller_headers, f"/api/products/{session_state.get('created_product_id')}"),
        (admin_headers, f"/api/admin/brands/{session_state.get('created_brand_id')}"),
        (admin_headers, f"/api/admin/categories/{session_state.get('created_category_id')}"),
        (admin_headers, f"/api/admin/coupons/{session_state.get('created_coupon_id')}"),
        (admin_headers, f"/api/admin/campaigns/{session_state.get('created_campaign_id')}"),
    ]

    for headers, endpoint in delete_operations:
        if "/None" in endpoint or endpoint.endswith("/"):
            continue
        try:
            response = client.delete(endpoint, headers=headers)
            if response.status_code not in (200, 204, 404):
                click.echo(f"  WARN: DELETE {endpoint} → {response.status_code}", err=True)
        except Exception:
            pass

    client.close()
    auth.close()

    # Snapshot'ları da temizle
    snapshot_manager.clear_baseline()
    session_state.reset()


if __name__ == "__main__":
    cli()
