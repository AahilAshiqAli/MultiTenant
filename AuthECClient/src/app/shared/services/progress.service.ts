import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root',
})
export class ProgressService {
  private hubConnection!: signalR.HubConnection;

  constructor(private authService: AuthService) {}

  public startConnection() {
    this.hubConnection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5007/progressHub', {
      accessTokenFactory: () => this.authService.getToken() || ''
    })
      .build();

    this.hubConnection.start().catch(err => console.log('Error: ', err));
  }

  public onProgress(callback: (progress: number) => void) {
    this.hubConnection.on('ConversionProgress', progress => {
      callback(progress);
    });
  }
}
