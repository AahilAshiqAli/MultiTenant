import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../shared/services/auth.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-tenant',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './tenant.component.html',
  styles: ``
})
export class TenantComponent {
  successMessage: string | null = null;
  constructor(
    public formBuilder: FormBuilder,
    private service: AuthService,
    private router: Router,
    private toastr: ToastrService) { }

    ngOnInit(): void {
      if (this.service.isLoggedIn())
        this.router.navigateByUrl('/dashboard')
    }
    isSubmitted: boolean = false;

    form = this.formBuilder.group({
      name: ['', Validators.required]
    })
  
    hasDisplayableError(controlName: string): Boolean {
      const control = this.form.get(controlName);
      return Boolean(control?.invalid) &&
        (this.isSubmitted || Boolean(control?.touched) || Boolean(control?.dirty))
    }
  
    onSubmit() {
      this.isSubmitted = true;
      if (this.form.valid) {
        this.service.createTenant(this.form.value).subscribe({
          next: (res: any) => {
            this.successMessage = `Tenant created: Name = ${res.name}, ID = ${res.tenantID}`;

            // Optional: delay 2 seconds before navigating
            setTimeout(() => {
              this.router.navigate(['/signup'], { queryParams: { tenantId: res.tenantID } });
            }, 2000);
          },
          error: err => {
            if (err.status == 400)
              this.toastr.error('Error in Server.', 'Creation failed')
            else
              console.log('error during login:\n', err);
          }
        })
      }
    } 
  

}
