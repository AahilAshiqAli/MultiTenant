import { Component, OnInit } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../shared/services/auth.service';
import { UserService } from '../shared/services/user.service';
import { HideIfClaimsNotMetDirective } from '../directives/hide-if-claims-not-met.directive';
import { claimReq } from '../shared/utils/claimReq-utils';
import { FormsModule } from '@angular/forms'; 
import { CommonModule } from '@angular/common';


@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [HideIfClaimsNotMetDirective, CommonModule, FormsModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styles: ``
})
export class DashboardComponent implements OnInit {

  constructor(private router: Router,
    private authService: AuthService,
    private userService: UserService) { }
  fullName: string = ''
  claimReq = claimReq

  currentTenant: string = 'tenant1';
  notifications: number = 3;
  userRole: string = 'Admin';

  statsList = [
    { label: 'Users', value: 0, icon: 'bi bi-people'},
    { label: 'Files', value: 0, icon: 'bi bi-chat-dots'},
  ];
  ngOnInit(): void {
    this.userService.getUserProfile().subscribe({
      next: (res: any) => this.fullName = res.fullName,
      error: (err: any) => console.log('error while retrieving user profile:\n', err)
    })

    this.userService.getTenantUserCount().subscribe({
      next: (res: any) => this.statsList[0].value = res.userCount,
      error: (err: any) => console.log('error while retrieving user profile:\n', err)
    })

    this.userService.getTenantFileCount().subscribe({
      next: (res: any) => {
        this.statsList[1].value = res.fileCount
      },
      error: (err: any) => console.log('error while retrieving file profile:\n', err)
    })
  }




  actionCards = [
    {
      title: 'Upload Files',
      desc: 'Upload to tenant-isolated S3 buckets with automatic indexing',
      cta: 'Start Now',
      link: '/fileupload',
      icon: 'bi bi-rocket-takeoff',
      bg: 'bg-primary'
    },
    {
      title: 'Search Data',
      desc: 'Cross-tenant search with role-based access control',
      cta: 'Go to Search',
      link: '/search',
      icon: 'bi bi-person-gear',
      bg: 'bg-success'
    }
  ];




}
