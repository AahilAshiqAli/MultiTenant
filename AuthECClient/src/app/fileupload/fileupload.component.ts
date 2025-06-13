import {Form, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import {HttpClient, HttpEvent, HttpEventType } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { CommonModule } from '@angular/common';
import { ElementRef, ViewChild, Component, OnInit } from '@angular/core';
import { ProgressService } from '../shared/services/progress-service.service';



@Component({
  selector: 'app-fileupload',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './fileupload.component.html',
  styles: ``
})
export class FileuploadComponent implements OnInit {
  uploadForm: FormGroup;
  selectedFile: File | null = null;
  uploadProgress: number | null = null;
  uploadResponse: string | null = null;
  uploadedSize: number = 0;
  conversionProgress: number | null = null;
  totalBytes: number = 0;
  validationError: string = '';
  private readonly MAX_SIZE = 2 * 1024 * 1024 * 1024; // 2 GB
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  constructor(private fb: FormBuilder, private http: HttpClient, private progressService: ProgressService) {
    this.uploadForm = this.fb.group({});
  }

  ngOnInit(): void {
    this.progressService.startConnection();

    this.progressService.onProgress((progress: number) => {
      this.uploadProgress = progress;
    });
  }

  openFileBrowser() {
    this.fileInput.nativeElement.click();
  }

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.validationError = '';
    this.uploadResponse = null;
    this.uploadProgress = null;
    this.uploadedSize = 0;
    this.totalBytes = 0;
  
    if (!file) return;
  
    if (!/^(audio|video)\//.test(file.type)) {
      this.validationError = 'Only audio or video files are allowed.';
      return;
    }
  
    if (file.size > this.MAX_SIZE) {
      this.validationError = `File too large. Max size is ${this.formatBytes(this.MAX_SIZE)}.`;
      return;
    }
  
    this.selectedFile = file;
    this.totalBytes = file.size;
  }

  onSubmit(): void {
    console.log("hello");
    if (!this.selectedFile) return;
  
    const formData = new FormData();
    formData.append('file', this.selectedFile);
    formData.append('name', this.selectedFile.name);
  
    this.http.post(environment.apiBaseUrl + '/Products', formData, {
      reportProgress: true,
      observe: 'events'
    }).subscribe((event: HttpEvent<any>) => {
      if (event.type === HttpEventType.UploadProgress && event.total) {
        this.uploadedSize = event.loaded;
        this.uploadProgress = Math.round((event.loaded / event.total) * 100);
      } else if (event.type === HttpEventType.Response) {
        this.uploadResponse = event.body;

    
          // ✅ Start listening to backend conversion progress here
          this.progressService.onProgress((progress: number) => {
            this.conversionProgress = progress;
          });
      }
    });
  }
  
  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }
}
