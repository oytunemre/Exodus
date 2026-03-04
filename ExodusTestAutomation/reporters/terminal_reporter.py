"""
Rich kütüphanesiyle renkli terminal çıktısı.
"""
from typing import List, Dict, Any, Optional
from datetime import datetime

try:
    from rich.console import Console
    from rich.table import Table
    from rich.panel import Panel
    from rich import box
    from rich.text import Text
    RICH_AVAILABLE = True
except ImportError:
    RICH_AVAILABLE = False

console = Console() if RICH_AVAILABLE else None


def _status_style(code: Optional[int]) -> str:
    if code is None:
        return "red"
    if 200 <= code < 300:
        return "green"
    if 400 <= code < 500:
        return "yellow"
    return "red"


def _method_style(method: str) -> str:
    styles = {
        "GET": "cyan",
        "POST": "green",
        "PUT": "blue",
        "PATCH": "magenta",
        "DELETE": "red",
    }
    return styles.get(method, "white")


def print_run_summary(results: List[Dict[str, Any]], title: str = "Test Sonuçları") -> None:
    """Çalıştırma sonuçlarını terminal tablosunda göster."""
    if not RICH_AVAILABLE:
        _print_plain(results, title)
        return

    # Dashboard
    total = len(results)
    success = sum(1 for r in results if r.get("status_code") and 200 <= r["status_code"] < 300)
    client_errors = sum(1 for r in results if r.get("status_code") and 400 <= r["status_code"] < 500)
    server_errors = sum(1 for r in results if r.get("status_code") and r["status_code"] >= 500)
    errors = sum(1 for r in results if r.get("error"))
    times = [r["response_time_ms"] for r in results if r.get("response_time_ms")]
    avg_time = round(sum(times) / len(times), 1) if times else 0

    summary = (
        f"[bold]{title}[/bold] | "
        f"Toplam: [white]{total}[/white] | "
        f"[green]✅ {success} 2xx[/green] | "
        f"[yellow]⚠️  {client_errors} 4xx[/yellow] | "
        f"[red]❌ {server_errors} 5xx[/red] | "
        f"[red]💥 {errors} hata[/red] | "
        f"Ort. süre: [cyan]{avg_time}ms[/cyan]"
    )
    console.print(Panel(summary, expand=False))

    # Tablo
    table = Table(
        box=box.ROUNDED,
        show_header=True,
        header_style="bold white",
        expand=False,
    )
    table.add_column("Method", style="bold", width=8)
    table.add_column("Endpoint", min_width=40, max_width=70)
    table.add_column("Status", justify="center", width=8)
    table.add_column("Süre", justify="right", width=8)
    table.add_column("Tag", width=20)
    table.add_column("Diff", justify="center", width=6)

    for r in results:
        method = r["method"]
        code = r.get("status_code")
        has_diff = r.get("has_diff", False)
        is_new = r.get("is_new", False)

        method_text = Text(method, style=_method_style(method))
        status_text = Text(str(code) if code else r.get("error", "ERR"), style=_status_style(code))

        time_ms = r.get("response_time_ms")
        time_text = Text(f"{time_ms}ms" if time_ms else "-", style="dim")

        diff_text = Text("●" if has_diff else ("★" if is_new else "≈"), style="yellow" if has_diff else ("cyan" if is_new else "dim"))

        table.add_row(
            method_text,
            r["path"],
            status_text,
            time_text,
            r.get("tag", ""),
            diff_text,
        )

    console.print(table)
    console.print(f"[dim]Çalıştırma zamanı: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}[/dim]")


def print_scenario_summary(scenario_name: str, steps: List[Dict[str, Any]]) -> None:
    """Senaryo adımlarını göster."""
    if not RICH_AVAILABLE:
        print(f"\n=== {scenario_name} ===")
        for step in steps:
            icon = "✅" if step["success"] else ("⏭️" if step["skipped"] else "❌")
            print(f"  {icon} {step['name']}: {step.get('status_code', step.get('error', ''))}")
        return

    total = len(steps)
    success = sum(1 for s in steps if s.get("success"))
    skipped = sum(1 for s in steps if s.get("skipped"))
    failed = total - success - skipped

    title = (
        f"[bold]{scenario_name}[/bold] | "
        f"[green]✅ {success}[/green] | "
        f"[yellow]⏭️  {skipped} atlandı[/yellow] | "
        f"[red]❌ {failed}[/red]"
    )
    console.print(Panel(title, expand=False))

    table = Table(box=box.SIMPLE, show_header=True, header_style="bold")
    table.add_column("#", width=4)
    table.add_column("Adım", min_width=30)
    table.add_column("Status", justify="center", width=8)
    table.add_column("Süre", justify="right", width=8)
    table.add_column("Sonuç", width=6)

    for i, step in enumerate(steps, 1):
        if step.get("skipped"):
            icon = Text("⏭️", style="yellow")
            code_text = Text("skip", style="yellow dim")
        elif step.get("success"):
            icon = Text("✅", style="green")
            code = step.get("status_code", "")
            code_text = Text(str(code), style="green")
        else:
            icon = Text("❌", style="red")
            code = step.get("status_code", step.get("error", "ERR"))
            code_text = Text(str(code), style="red")

        time_ms = step.get("response_time_ms")
        time_text = Text(f"{time_ms}ms" if time_ms else "-", style="dim")

        table.add_row(str(i), step["name"], code_text, time_text, icon)

    console.print(table)


def _print_plain(results: List[Dict[str, Any]], title: str) -> None:
    """Rich olmadan sade çıktı."""
    print(f"\n{'='*60}")
    print(f" {title}")
    print(f"{'='*60}")
    for r in results:
        code = r.get("status_code", r.get("error", "ERR"))
        time_ms = r.get("response_time_ms", "")
        print(f"  [{r['method']:6}] {r['path']:<50} {code}  {time_ms}ms")

