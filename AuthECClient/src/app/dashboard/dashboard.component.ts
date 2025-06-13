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

  ngOnInit(): void {
    this.userService.getUserProfile().subscribe({
      next: (res: any) => this.fullName = res.fullName,
      error: (err: any) => console.log('error while retrieving user profile:\n', err)
    })
  }


  tenantData: { [key: string]: { name: string } } = {
    tenant1: { name: 'Tenant One' },
    tenant2: { name: 'Tenant Two' },
    tenant3: { name: 'Tenant Three' }
  };

  statsList = [
    { label: 'Users', value: 150, icon: 'bi bi-people', note: 'Updated 5 mins ago' },
    { label: 'Messages', value: 480, icon: 'bi bi-chat-dots', note: 'Updated 10 mins ago' },
    { label: 'Jobs Queued', value: 32, icon: 'bi bi-hourglass-split', note: '5 pending, 27 running' },
    { label: 'Errors', value: 4, icon: 'bi bi-exclamation-triangle', note: 'Critical attention needed' },
  ];

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
      title: 'Audit Logs',
      desc: 'Inspect user activity and tenant-level logs',
      cta: 'View Logs',
      link: '/audit',
      icon: 'bi bi-clipboard-data',
      bg: 'bg-warning'
    },
    {
      title: 'Search Data',
      desc: 'Cross-tenant search with role-based access control',
      cta: 'Go to Search',
      link: '/access',
      icon: 'bi bi-person-gear',
      bg: 'bg-success'
    }
  ];

  recentJobs = [
    { name: 'Invoice Sync', time: '10 mins ago', status: 'Completed', priority: 'High' },
    { name: 'Data Backup', time: '1 hour ago', status: 'In Progress', priority: 'Medium' },
    { name: 'Report Generation', time: 'Today', status: 'Failed', priority: 'Critical' }
  ];

  recentActivity = [
    {
      action: 'User Login',
      details: 'Admin user logged into tenant dashboard',
      time: '5 mins ago',
      user: 'Alice'
    },
    {
      action: 'Role Changed',
      details: 'Moderator promoted to Admin role',
      time: '30 mins ago',
      user: 'Bob'
    },
    {
      action: 'Job Created',
      details: 'Monthly billing report generated',
      time: '2 hours ago',
      user: 'Charlie'
    }
  ];

  configStatus = [
    { title: 'Database', status: 'Connected' },
    { title: 'Message Queue', status: 'Active' },
    { title: 'Logging', status: 'Enabled' },
    { title: 'File Storage', status: 'Configured' }
  ];

  get tenantKeys(): string[] {
    return Object.keys(this.tenantData);
  }


  getPriorityClass(priority: string): string {
    switch (priority) {
      case 'High':
        return 'border-warning bg-light';
      case 'Critical':
        return 'border-danger bg-light';
      case 'Medium':
        return 'border-secondary bg-light';
      default:
        return 'border-light bg-white';
    }
  }

  getStatusIcon(status: string): string {
    switch (status) {
      case 'Completed':
        return 'bi bi-check-circle-fill text-success';
      case 'In Progress':
        return 'bi bi-arrow-repeat text-warning';
      case 'Failed':
        return 'bi bi-x-circle-fill text-danger';
      default:
        return 'bi bi-info-circle-fill text-secondary';
    }
  }

  getStatusBadge(status: string): string {
    switch (status) {
      case 'Completed':
        return 'bg-success';
      case 'In Progress':
        return 'bg-warning text-dark';
      case 'Failed':
        return 'bg-danger';
      default:
        return 'bg-secondary';
    }
  }


}
