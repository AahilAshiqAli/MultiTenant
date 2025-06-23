import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../shared/services/auth.service';
import { ToastrService } from 'ngx-toastr';
import { RegistrationComponent } from "../registration/registration.component";
import { FirstKeyPipe } from '../../shared/pipes/first-key.pipe';

@Component({
  selector: 'app-tenant',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule,  FirstKeyPipe, RouterLink],
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

    tenantForm = this.formBuilder.group({
      name: ['', Validators.required],
      provider: ['azure', Validators.required],
      container: ['', Validators.required],
      enableVersioning: [false],
      retentionDays: ['', [Validators.required, Validators.min(1)]],
      defaultBlobTier: ['Hot', Validators.required]
    });

    
    
    hasDisplayableError(controlName: string): Boolean {
      const control = this.tenantForm.get(controlName);
      return Boolean(control?.invalid) &&
      (this.isSubmitted || Boolean(control?.touched) || Boolean(control?.dirty))
    }
    
    passwordMatchValidator: ValidatorFn = (control: AbstractControl): null => {
      const password = control.get('password')
      const confirmPassword = control.get('confirmPassword')
      
      if (password && confirmPassword && password.value != confirmPassword.value)
      confirmPassword?.setErrors({ passwordMismatch: true })
      else
      confirmPassword?.setErrors(null)
      
      return null;
    }
    
    form = this.formBuilder.group({
      fullName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [
        Validators.required,
        Validators.minLength(6),
        Validators.pattern(/(?=.*[^a-zA-Z0-9 ])/)
      ]],
      confirmPassword: [''],
      gender: ['', Validators.required],
      dob: ['', Validators.required],
      role: ['Admin', Validators.required]
    }, { validators: this.passwordMatchValidator });


onSubmit() {
  this.isSubmitted = true;

  // Mark both forms as touched to trigger validation messages
  this.tenantForm.markAllAsTouched();
  this.form.markAllAsTouched();

  if (this.tenantForm.invalid || this.form.invalid) {
    this.toastr.error('Please fix form errors before submitting');
    return;
  }

  const combinedPayload = {
    ...this.tenantForm.value,
    user: this.form.value
  };

  this.service.createTenant(combinedPayload).subscribe({
    next: (res: any) => {
      this.successMessage = `Tenant created: Name = ${res.name}, ID = ${res.tenantID}`;

      // Optionally reset both forms
      this.tenantForm.reset();
      this.form.reset();
      this.isSubmitted = false;

      setTimeout(() => {
        this.router.navigate(['/signin'], { queryParams: { tenantId: res.tenantID } });
      }, 5000);
    },
    error: err => {
      if (err.error?.errors) {
        err.error.errors.forEach((x: any) => {
          switch (x.code) {
            case "DuplicateUserName":
              this.toastr.error('Username is already taken.', 'Registration Failed');
              break;

            case "DuplicateEmail":
              this.toastr.error('Email is already taken.', 'Registration Failed');
              break;

            default:
              this.toastr.error('Contact the developer.', 'Registration Failed');
              console.log('Unhandled error:', x);
              break;
          }
        });
      } else {
        this.toastr.error('Unknown error occurred.', 'Registration Failed');
        console.error('Error:', err);
      }
    }
  });
}

  

}
