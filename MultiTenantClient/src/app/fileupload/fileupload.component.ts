import {Form, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import {HttpClient, HttpEvent, HttpEventType } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { CommonModule } from '@angular/common';
import { ElementRef, ViewChild, Component, OnInit } from '@angular/core';
import { LogService } from '../shared/services/log.service';
import { ProgressService } from '../shared/services/progress.service';



@Component({
  selector: 'app-fileupload',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule, FormsModule],
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
  tenantLogs: any[] = [];
  private readonly MAX_SIZE = 2 * 1024 * 1024 * 1024; 
  private readonly resolutionMap: {
    [key in '480p' | '720p' | '1080p' | '2160p']: { width: number; height: number }
  } = {
    '480p': { width: 854, height: 480 },
    '720p': { width: 1280, height: 720 },
    '1080p': { width: 1920, height: 1080 },
    '2160p': { width: 3840, height: 2160 }
  };
  showLogs: boolean = false;
  showRendition: boolean = false;
  videoResolution: any = null;
  allowSubmit: boolean = false;

  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  constructor(private fb: FormBuilder, private http: HttpClient, private logService: LogService, private progressService: ProgressService) {
    this.uploadForm = this.fb.group({
      isPrivate: [false],
      rendition : [null]
    });
  }

  ngOnInit(): void {
    this.logService.startConnection(); 


    this.logService.log$.subscribe((log: any) => {
      this.tenantLogs.push(log);
      // Optionally auto-scroll to latest
      setTimeout(() => {
        const logBox = document.getElementById('logBox');
        if (logBox) {
          logBox.scrollTop = logBox.scrollHeight;
        }
      }, 100);
    });

    this.uploadForm.get('rendition')?.valueChanges.subscribe((value) => {
      this.validateResolution(value);
      console.log(value)
    });
  }

  toggleLogs() {
    this.showLogs = !this.showLogs;
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

    const fileType = file.type.split('/')[0];
    if (fileType == 'video') {
      const video = document.createElement('video');
      video.preload = 'metadata';
  
      video.onloadedmetadata = () => {
        const width = video.videoWidth;
        const height = video.videoHeight;
        console.log(`Video resolution: ${width}x${height}`);
  
        this.videoResolution = { width, height }; // Store it if needed
        this.showRendition = true;
        console.log(this.videoResolution)

      };
  
      video.onerror = () => {
        this.validationError = 'Unable to read video metadata.';
      };
  
      video.src = URL.createObjectURL(file);
  
    }

    if (fileType == 'audio') {
      this.allowSubmit = true;
    }
  
    if (file.size > this.MAX_SIZE) {
      this.validationError = `File too large. Max size is ${this.formatBytes(this.MAX_SIZE)}.`;
      return;
    }
  
    this.selectedFile = file;
    this.totalBytes = file.size;
  }

  validateResolution(rendition : '480p' | '720p' | '1080p' | '2160p' ) {
    const minRequired = this.resolutionMap[rendition];
    const { width, height } =  this.videoResolution;

    const resolutionIsValid =
    (width >= minRequired.width && height >= minRequired.height) ||
    (width >= minRequired.height && height >= minRequired.width);

    if (!resolutionIsValid) {
      this.validationError = `Change video resolution. Must be at least ${minRequired.width} × ${minRequired.height}.`;
      this.allowSubmit = false;
      return;
    }
    this.allowSubmit = true
    this.validationError = '';
  }



  onSubmit(): void {
    if (!this.selectedFile) return;
  
    const formData = new FormData();
    formData.append('file', this.selectedFile);
    formData.append('name', this.selectedFile.name);
    const isPrivate = this.uploadForm.get('isPrivate')?.value ?? false;
    formData.append('isPrivate', isPrivate.toString().toLowerCase());
    const rendition = this.uploadForm.get('rendition')?.value ?? "480p";
    formData.append('rendition', rendition);
    console.log(rendition, isPrivate);

    this.http.post(environment.apiBaseUrl + '/Contents', formData, {
      reportProgress: true,
      observe: 'events'
    }).subscribe((event: HttpEvent<any>) => {
      if (event.type === HttpEventType.UploadProgress && event.total) {
        this.uploadedSize = event.loaded;
        this.uploadProgress = Math.round((event.loaded / event.total) * 100);
      } else if (event.type === HttpEventType.Response) {
        this.uploadResponse = event.body;
        this.progressService.startPolling((progress) => {
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
