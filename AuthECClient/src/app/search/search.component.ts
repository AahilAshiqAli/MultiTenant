import { Component, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpEventType } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './search.component.html',
  styles: []
})
export class SearchComponent {
  searchQuery: string = '';
  suggestions: string[] = [];
  results: string[] = [];
  showCards: boolean = false;

  allItems: string[] = [];

  pageSize = 6;
  currentPage = 1;
  pagedResults: string[] = [];
  totalPages = 0;

  constructor(private http: HttpClient) {
    this.suggestions = this.allItems;
  }

  onSearchChange(): void {
    this.showCards = false;
    const query = this.searchQuery.trim().toLowerCase();
  
    if (query.length > 3) {
      this.http.get<any[]>(
        `${environment.apiBaseUrl}/Products/by-name?name=${query}`,
        {
          observe: 'events',
          reportProgress: true
        }
      ).subscribe(event => {
        if (event.type === HttpEventType.Response) {
          const data = event.body;
          this.allItems = data?.map((p: any) => p.name) ?? [];
          this.suggestions = this.allItems.filter(item =>
            item.toLowerCase().includes(query)
          );
        }
      });
    } else {
      this.suggestions = [];
    }
  }

  onEnterPressed(): void {
    const query = this.searchQuery.trim().toLowerCase();
    this.results = this.allItems.filter(item =>
      item.toLowerCase().includes(query)
    );
    this.totalPages = Math.ceil(this.results.length / this.pageSize);
    this.currentPage = 1;
    this.updatePagedResults();
    this.showCards = true;
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

  @HostListener('document:keydown.enter', ['$event'])
  handleEnter(event: KeyboardEvent) {
    this.onEnterPressed();
  }
}
