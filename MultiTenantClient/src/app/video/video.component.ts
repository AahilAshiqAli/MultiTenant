import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-video',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './video.component.html',
  styles: ``
})
export class DisplayVideoComponent implements OnInit {
  videoId: number | null = null;
  videoUrl: string | null = null;
  currentTime: number = 0;
  duration: number = 0;

  @ViewChild('videoPlayer') videoPlayer!: ElementRef<HTMLVideoElement>;

  constructor(private route: ActivatedRoute, private http: HttpClient) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const idParam = params.get('id');
      const parsedId = idParam !== null ? Number(idParam) : NaN;

      if (!isNaN(parsedId)) {
        this.videoId = parsedId;
        this.loadVideo();
      } else {
        console.error('Invalid video ID');
        this.videoId = null;
      }
    });
  }

  loadVideo() {
    const token = localStorage.getItem('access_token');
  

    this.http.get(`http://localhost:5007/api/Contents/stream/${this.videoId}`, {
      responseType: 'text' 
    }).subscribe((url: string) => {
      this.videoUrl = url; 
    }, err => {
      console.error('Video load failed', err);
    });
  }

  onTimeUpdate(event: Event) {
    const video = event.target as HTMLVideoElement;
    this.currentTime = video.currentTime;
    this.duration = video.duration;
  }

  formatTime(seconds: number): string {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${this.padZero(mins)}:${this.padZero(secs)}`;
  }
  
  padZero(num: number): string {
    return num < 10 ? '0' + num : '' + num;
  }
}
