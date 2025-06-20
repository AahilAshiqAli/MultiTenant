import { Component, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpEventType } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { RouterLink } from '@angular/router';

interface SearchResult {
  Id: number
  tenantId: string;
  size: number;
  contentType: string;
  fileName: string;
  thumbnail: string;
}

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './search.component.html',
  styles: []
})
export class SearchComponent implements OnInit {
  searchQuery: string = '';
  suggestions: string[] = [];
  results: SearchResult[] = [];
  pagedResults: SearchResult[] = [];
  allItems: string[] = [];

  showCards: boolean = false;
  pageSize = 6;
  currentPage = 1;
  totalPages = 0;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.http.get<any[]>(
      `${environment.apiBaseUrl}/Contents/`,
      {
        observe: 'events',
        reportProgress: true
      }
    ).subscribe(event => {
      if (event.type === HttpEventType.Response) {
        const data = event.body ?? [];
        this.results = data.map((p: any) => ({
          Id: p.id,
          tenantId: p.tenantId,
          size: p.size,
          contentType: p.contentType,
          fileName: p.fileName,
          thumbnail: p.thumbnail
        }));

        

        this.totalPages = Math.ceil(this.results.length / this.pageSize);
        this.currentPage = 1;
        this.updatePagedResults();
        this.showCards = true;
      }
    });
  }

  onSearchChange(): void {
    this.showCards = false;
    const query = this.searchQuery.trim().toLowerCase();

    if (query.length > 3) {
      this.http.get<any[]>(
        `${environment.apiBaseUrl}/Contents/?name=${query}`,
        {
          observe: 'events',
          reportProgress: true
        }
      ).subscribe(event => {
        if (event.type === HttpEventType.Response) {
          const data = event.body ?? [];

          this.allItems = data.map((p: any) => p.fileName);
          this.suggestions = this.allItems;
        }
      });
    } else {
      this.suggestions = [];
    }
  }

  onEnterPressed(): void {
    const query = this.searchQuery.trim().toLowerCase();

    this.http.get<any[]>(
      `${environment.apiBaseUrl}/Contents/?name=${query}`,
      {
        observe: 'events',
        reportProgress: true
      }
    ).subscribe(event => {
      if (event.type === HttpEventType.Response) {
        const data = event.body ?? [];
        this.results = data.map((p: any) => ({
          Id: p.id,
          tenantId: p.tenantId,
          size: p.size,
          contentType: p.contentType,
          fileName: p.fileName,
          thumbnail: p.thumbnail
        }));

        

        this.totalPages = Math.ceil(this.results.length / this.pageSize);
        this.currentPage = 1;
        this.updatePagedResults();
        this.showCards = true;
      }
    });
  }

  updatePagedResults(): void {
    const start = (this.currentPage - 1) * this.pageSize;
    const end = start + this.pageSize;
    this.pagedResults = this.results.slice(start, end);
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.updatePagedResults();
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.currentPage++;
      this.updatePagedResults();
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.updatePagedResults();
    }
  }

  clearSearch(): void {
    this.searchQuery = '';
    this.suggestions = this.allItems;
    this.results = [];
    this.pagedResults = [];
    this.showCards = false;
    this.currentPage = 1;
  }

  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  @HostListener('document:keydown.enter', ['$event'])
  handleEnter(event: KeyboardEvent): void {
    this.onEnterPressed();
  }
}
