import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class UserService {

  constructor(private http: HttpClient,
    private authService: AuthService) { }

  getUserProfile() {
    return this.http.get(environment.apiBaseUrl + '/Account/UserProfile')
  }

  getTenantUserCount() {
    return this.http.get<{ tenantId: string; userCount: number }>(
      `${environment.apiBaseUrl}/Account/TenantUserCount`
    );
  }

  getTenantFileCount() {
    return this.http.get<{ tenantId: string; userCount: number }>(
      `${environment.apiBaseUrl}/Account/TenantFileCount`
    );
  }

  getFilesCount(){
    return 0
  }
}
