import { Component } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from '../../shared/services/auth.service';
import { HideIfClaimsNotMetDirective } from '../../directives/hide-if-claims-not-met.directive';
import { claimReq } from "../../shared/utils/claimReq-utils";

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, HideIfClaimsNotMetDirective],
  templateUrl: './main-layout.component.html',
  styles: ``
})
export class MainLayoutComponent {
  constructor(private router: Router,
    private authService: AuthService) { }
    
    claimReq = claimReq
    
    onLogout() {
      this.authService.deleteToken();
      this.router.navigateByUrl('/signin');
    }

    onDelete() {
      if (confirm('Are you sure you want to delete this item?')) {
        const result = this.authService.deleteAccount();
        this.router.navigateByUrl('/signin');
      } else {
        // Cancelled
      }
    }
  }
