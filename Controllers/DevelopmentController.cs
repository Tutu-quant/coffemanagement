using Microsoft.AspNetCore.Mvc;
using Quản_lý_quán_cafe.Services;

namespace Quản_lý_quán_cafe.Controllers;

/// <summary>
/// Development-only controller for testing and demonstration purposes.
/// Only available in Development environment.
/// </summary>
[Route("api/dev")]
public class DevelopmentController : Controller
{
    private readonly IWebHostEnvironment _environment;
    private readonly DemoDataSeeder _demoSeeder;
    private readonly ILogger<DevelopmentController> _logger;

    public DevelopmentController(
        IWebHostEnvironment environment,
        DemoDataSeeder demoSeeder,
        ILogger<DevelopmentController> logger)
    {
        _environment = environment;
        _demoSeeder = demoSeeder;
        _logger = logger;
    }

    /// <summary>
    /// Seeds demo data for kitchen board and dashboard testing.
    /// Only available in Development environment.
    /// </summary>
    [HttpPost("seed-demo")]
    public async Task<IActionResult> SeedDemoData(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        try
        {
            var result = await _demoSeeder.SeedDemoDataAsync(cancellationToken);
            if (result.Success)
            {
                _logger.LogInformation("Demo data seeded: {Message}", result.Message);
                return Ok(new { success = true, message = result.Message });
            }
            else
            {
                return BadRequest(new { success = false, error = result.ErrorMessage });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding demo data");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Clears all demo data (marked with CreatedBy = "DemoSeeder").
    /// Only available in Development environment.
    /// </summary>
    [HttpPost("clear-demo")]
    public async Task<IActionResult> ClearDemoData(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        try
        {
            var result = await _demoSeeder.ClearDemoDataAsync(cancellationToken);
            if (result.Success)
            {
                _logger.LogInformation("Demo data cleared: {Message}", result.Message);
                return Ok(new { success = true, message = result.Message });
            }
            else
            {
                return BadRequest(new { success = false, error = result.ErrorMessage });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing demo data");
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Gets development tools UI (for manual seeding/clearing).
    /// Only available in Development environment.
    /// </summary>
    [HttpGet("tools")]
    public IActionResult Tools()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var html = @"<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>🔧 Demo Tools - BrewPoint Development</title>
    <link href='https://cdnjs.cloudflare.com/ajax/libs/bootstrap/5.3.0/css/bootstrap.min.css' rel='stylesheet'>
    <link href='https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css' rel='stylesheet'>
    <style>
        :root {
            --coffee-950: #2D1B14;
            --coffee-900: #351F12;
            --caramel-500: #C57A24;
            --cream-50: #FBF8EF;
        }
        html, body { height: 100%; margin: 0; padding: 0; background: linear-gradient(135deg, var(--coffee-950) 0%, var(--coffee-900) 100%); font-family: 'Inter', system-ui, -apple-system, 'Segoe UI', Roboto, 'Helvetica Neue', Arial; }
        .demo-container { min-height: 100vh; display: flex; align-items: center; justify-content: center; padding: 20px; }
        .demo-card { background: white; border-radius: 16px; padding: 40px; box-shadow: 0 20px 60px rgba(0, 0, 0, 0.4); max-width: 600px; width: 100%; }
        .demo-header { text-align: center; margin-bottom: 40px; }
        .demo-header h1 { color: var(--coffee-950); font-weight: 700; font-size: 32px; margin-bottom: 8px; display: flex; align-items: center; justify-content: center; gap: 12px; }
        .demo-header .subtitle { color: #7E6047; font-size: 14px; font-weight: 500; }
        .info-section { background: var(--cream-50); border-left: 4px solid var(--caramel-500); padding: 20px; border-radius: 8px; margin-bottom: 30px; }
        .info-section h5 { color: var(--coffee-950); font-weight: 700; margin-bottom: 15px; display: flex; align-items: center; gap: 8px; }
        .info-section ul { list-style: none; padding: 0; margin: 0; }
        .info-section li { color: #7E6047; font-size: 13px; margin: 8px 0; padding-left: 24px; position: relative; }
        .info-section li:before { content: '→'; position: absolute; left: 0; color: var(--caramel-500); font-weight: 600; }
        .btn-demo { padding: 14px 28px; font-weight: 700; border-radius: 10px; border: none; cursor: pointer; transition: all 0.3s ease; width: 100%; margin: 10px 0; font-size: 15px; display: flex; align-items: center; justify-content: center; gap: 10px; }
        .btn-seed { background: linear-gradient(135deg, #27AE60 0%, #229954 100%); color: white; }
        .btn-seed:hover { transform: translateY(-3px); box-shadow: 0 10px 25px rgba(39, 174, 96, 0.4); }
        .btn-clear { background: linear-gradient(135deg, #E74C3C 0%, #C0392B 100%); color: white; }
        .btn-clear:hover { transform: translateY(-3px); box-shadow: 0 10px 25px rgba(231, 76, 60, 0.4); }
        .status-message { margin-top: 20px; padding: 15px 20px; border-radius: 8px; display: none; }
        .status-success { background: #D5F4E6; color: #27AE60; border: 1px solid #27AE60; }
        .status-error { background: #FADBD8; color: #E74C3C; border: 1px solid #E74C3C; }
        .btn-demo:disabled { opacity: 0.6; cursor: not-allowed; }
        .footer-links { margin-top: 30px; padding-top: 20px; border-top: 1px solid #EFE3C6; display: flex; gap: 10px; justify-content: center; flex-wrap: wrap; }
        .footer-links a { padding: 8px 16px; border-radius: 8px; border: 1px solid var(--caramel-500); color: var(--caramel-500); text-decoration: none; font-size: 13px; font-weight: 600; transition: all 0.3s ease; }
        .footer-links a:hover { background: var(--caramel-500); color: white; }
    </style>
</head>
<body>
    <div class='demo-container'>
        <div class='demo-card'>
            <div class='demo-header'>
                <h1><i class='bi bi-wrench-adjustable'></i> Demo Tools</h1>
                <div class='subtitle'>Development Environment Only</div>
            </div>

            <div class='info-section'>
                <h5><i class='bi bi-list-check'></i> Demo Data Bao Gồm:</h5>
                <ul>
                    <li><strong>Order 1:</strong> Pending (3 phút) - Normal</li>
                    <li><strong>Order 2:</strong> Pending (12 phút) - High Priority ⚠️</li>
                    <li><strong>Order 3:</strong> Preparing (17 phút) - Urgent ⏱️</li>
                    <li><strong>Order 4:</strong> Preparing (23 phút) - Critical 🔥</li>
                    <li style='margin-top: 12px;'><strong>Reservation 1:</strong> 5 phút quá hạn 🔴</li>
                    <li><strong>Reservation 2:</strong> 20 phút quá hạn (sắp bị auto-cancel) 🔴</li>
                    <li><strong>Reservation 3:</strong> Sắp tới (10 phút) ⏰</li>
                </ul>
            </div>

            <button class='btn-demo btn-seed' id='btnSeed' onclick='seedDemo()'>
                <i class='bi bi-plus-circle'></i> Tạo Dữ Liệu Demo
            </button>
            <button class='btn-demo btn-clear' id='btnClear' onclick='clearDemo()'>
                <i class='bi bi-trash3'></i> Xóa Dữ Liệu Demo
            </button>

            <div class='status-message' id='statusMessage'></div>

            <div class='footer-links'>
                <a href='/Cashier/Dashboard' target='_blank'>
                    <i class='bi bi-speedometer2'></i> Dashboard
                </a>
                <a href='/Cashier/Kitchen' target='_blank'>
                    <i class='bi bi-fire'></i> Kitchen
                </a>
            </div>
        </div>
    </div>

    <script>
        async function seedDemo() {
            if (!confirm('🔨 Tạo dữ liệu demo?\n\n(Sẽ xóa dữ liệu demo cũ nếu có)')) return;
            setLoading(true);
            try {
                const response = await fetch('/api/dev/seed-demo', { method: 'POST' });
                const data = await response.json();
                if (response.ok) {
                    showStatus('✅ ' + data.message, 'success');
                    setTimeout(() => {
                        window.location.href = '/Cashier/Dashboard';
                    }, 2000);
                } else {
                    showStatus('❌ ' + (data.error || 'Lỗi không xác định'), 'error');
                }
            } catch (error) {
                showStatus('❌ Lỗi kết nối: ' + error.message, 'error');
            } finally {
                setLoading(false);
            }
        }

        async function clearDemo() {
            if (!confirm('🗑️ Xóa tất cả dữ liệu demo?\n\n(Chỉ xóa demo data, không ảnh hưởng dữ liệu thật)')) return;
            setLoading(true);
            try {
                const response = await fetch('/api/dev/clear-demo', { method: 'POST' });
                const data = await response.json();
                if (response.ok) {
                    showStatus('✅ ' + data.message, 'success');
                    setTimeout(() => {
                        location.reload();
                    }, 2000);
                } else {
                    showStatus('❌ ' + (data.error || 'Lỗi không xác định'), 'error');
                }
            } catch (error) {
                showStatus('❌ Lỗi kết nối: ' + error.message, 'error');
            } finally {
                setLoading(false);
            }
        }

        function setLoading(loading) {
            const btnSeed = document.getElementById('btnSeed');
            const btnClear = document.getElementById('btnClear');
            btnSeed.disabled = loading;
            btnClear.disabled = loading;
        }

        function showStatus(message, type) {
            const statusEl = document.getElementById('statusMessage');
            statusEl.textContent = message;
            statusEl.className = 'status-message status-' + type;
            statusEl.style.display = 'block';
        }
    </script>
</body>
</html>";

        return Content(html, "text/html");
    }
}
