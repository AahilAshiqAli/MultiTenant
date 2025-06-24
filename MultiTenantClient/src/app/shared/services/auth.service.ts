import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { TOKEN_KEY } from '../constants';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  constructor(private http: HttpClient) { }
  
  createUser(formData: any) {
    //WARNING!
    //default value for Role, Gender, Age, LibraryID?
    //instead of registration form, there should some other
    //form to update these details of the user
    formData.gender = formData.gender.toLowerCase()
    const dob = new Date(formData.dob);
    const today = new Date();
    
    let age = today.getFullYear() - dob.getFullYear();
    const monthDiff = today.getMonth() - dob.getMonth();
    const dayDiff = today.getDate() - dob.getDate();
    
    // Adjust age if birthday hasn't occurred yet this year
    if (monthDiff < 0 || (monthDiff === 0 && dayDiff < 0)) {
      age--;
    }
    
    formData.age = age.toString();
    return this.http.post(environment.apiBaseUrl + '/Identity/signup', formData);
  }
  
  signin(formData: any) {
    return this.http.post(environment.apiBaseUrl + '/Identity/signin', formData);
  }

  createTenant(formData: any) {
    const flatPayload = {
      name: formData.name,
      provider: formData.provider,
      container: formData.container,
      enableVersioning: formData.enableVersioning,
      retentionDays: formData.retentionDays,
      defaultBlobTier: formData.defaultBlobTier,
      
      email: formData.user.email,
      password: formData.user.password,
      fullName: formData.user.fullName,
      gender: formData.user.gender,
      
      age: this.calculateAge(formData.user.dob),
      
    };
    console.log(flatPayload)
    return this.http.post(environment.apiBaseUrl + '/Identity/tenant-create', flatPayload);
  }
  
  isLoggedIn() {
    return this.getToken() != null ? true : false;
  }
  
  saveToken(token: string) {
    localStorage.setItem(TOKEN_KEY, token)
  }
  
  getToken() {
    return localStorage.getItem(TOKEN_KEY);
  }
  
  deleteToken() {
    localStorage.removeItem(TOKEN_KEY);
  }
  
  // deleteAccount() {
  //   this.http.delete(`${environment.apiBaseUrl}/Identity/delete/`)
  //   .subscribe({
  //     next: (res: any) => {
  //       this.toastr.success('User deleted', 'Success');
  //     },
  //     error: err => {
  //       console.error(err,"popopo",id);
  //       this.toastr.error('Failed to delete user', 'Error');
  //     }
  //   });
  // }
  
  getClaims(){
    return JSON.parse(window.atob(this.getToken()!.split('.')[1]))
  }
  
  private calculateAge(dob: string): number {
    const birthDate = new Date(dob);
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const m = today.getMonth() - birthDate.getMonth();
  
    if (m < 0 || (m === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }
  
    return age;
  }

}
