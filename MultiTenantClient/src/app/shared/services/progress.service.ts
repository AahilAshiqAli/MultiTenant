import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { interval, Subscription } from 'rxjs';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ProgressService {
  private pollSubscription?: Subscription;

  constructor(private http: HttpClient) {}

  startPolling(callback: (progress: number) => void): void {
    // Poll every 2 seconds
    this.pollSubscription = interval(2000).subscribe(() => {
      this.http.get<{ progress: number }>(environment.apiBaseUrl + '/progress')
        .subscribe({
          next: (res) => {
            callback(res.progress);

            // Stop polling automatically if 100% is reached
            if (res.progress >= 100) {
              this.stopPolling();
            }
          },
          error: (err) => {
            console.error('❌ Progress API error:', err);
          }
        });
    });
  }

  stopPolling(): void {
    if (this.pollSubscription) {
      this.pollSubscription.unsubscribe();
      this.pollSubscription = undefined;
    }
  }
}
