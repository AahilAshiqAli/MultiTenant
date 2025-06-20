import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-admin-only',
  standalone: true,
  imports: [],
  templateUrl: './admin-only.component.html',
  styles: ``
})
export class AdminOnlyComponent {
  constructor(private http: HttpClient) {}

  openLogsTab() {
    this.http.get<any[]>(` ${environment.apiBaseUrl}/admin/logs`).subscribe(logs => {
      const html = this.buildLogsHtml(logs);
      const blob = new Blob([html], { type: 'text/html' });
      const url = URL.createObjectURL(blob);
      window.open(url, '_blank');
    });
  }

  private buildLogsHtml(logs: any[]): string {
    const jsonData = JSON.stringify(logs, null, 2);
    return `
      <!DOCTYPE html>
      <html lang="en">
      <head>
        <meta charset="UTF-8">
        <title>Admin Logs Viewer</title>
        <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css">
        <style>
          body { padding: 20px; background-color: #f8f9fa; font-family: monospace; }
          pre { background-color: #fff; padding: 15px; border: 1px solid #ccc; border-radius: 4px; max-height: 80vh; overflow-y: auto; }
        </style>
      </head>
      <body>
        <h3>Admin Logs Viewer</h3>
        <div class="mb-3">
          <label for="levelFilter" class="form-label">Filter by Level:</label>
          <select class="form-select" id="levelFilter" onchange="filterLogs(this.value)">
            <option value="">All Levels</option>
            <option value="Information">Information</option>
            <option value="Warning">Warning</option>
            <option value="Error">Error</option>
            <option value="Debug">Debug</option>
          </select>
        </div>
        <pre id="logOutput">${jsonData}</pre>

        <script>
          const allLogs = ${jsonData};

          function filterLogs(level) {
            const filtered = level ? allLogs.filter(log => log.Level === level) : allLogs;
            document.getElementById('logOutput').textContent = JSON.stringify(filtered, null, 2);
          }
        </script>
      </body>
      </html>
    `;
  }
}