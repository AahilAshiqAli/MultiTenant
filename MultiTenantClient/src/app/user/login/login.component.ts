import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../shared/services/auth.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.component.html',
  styles: ``
})
export class LoginComponent implements OnInit {
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
    email: ['', Validators.required],
    password: ['', Validators.required],
  })

  hasDisplayableError(controlName: string): Boolean {
    const control = this.form.get(controlName);
    return Boolean(control?.invalid) &&
      (this.isSubmitted || Boolean(control?.touched) || Boolean(control?.dirty))
  }

  onSubmit() {
    this.isSubmitted = true;
    if (this.form.valid) {
      this.service.signin(this.form.value).subscribe({
        next: (res: any) => {
          this.service.saveToken(res.token);
          this.router.navigateByUrl('/dashboard');
        },
        error: err => {
          console.log(err.error.errors);
          if (err.error.errors)
            err.error.errors.forEach((x: any) => {
              switch (x.code) {

                case "UserNotFound":
                  this.toastr.error('Incorrect Email.', 'Login Failed')
                  break;

                case "IncorrectPassword":
                  this.toastr.error('Incorrect password.', 'Login Failed')
                  break;

                default:
                  this.toastr.error('Contact the developer', 'Login Failed')
                  break;
              }
            })
          else
            console.log('error:', err);
        }
      })
    }
  }

}
