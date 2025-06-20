import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

export interface SignalRLog {
  timestamp: string;
  level: string;
  message: string;
  exception: string | null;
  tenantId: string;
}

@Injectable({
  providedIn: 'root',
})
export class LogService {
  private hubConnection!: signalR.HubConnection;
  private logSubject = new Subject<any>();

  log$ = this.logSubject.asObservable();

  constructor(private authService: AuthService) {}

  public startConnection(): void {
    this.hubConnection = new signalR.HubConnectionBuilder() 
    .withUrl('http://localhost:5007/logHub', {
      accessTokenFactory: () => this.authService.getToken() || ''
    })
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('✅ LogHub connected'))
      .catch(err => console.error('❌ Error connecting LogHub:', err));

    this.hubConnection.on('ReceiveLog', (log) => {
      this.logSubject.next(log); 
    });
  }
}

