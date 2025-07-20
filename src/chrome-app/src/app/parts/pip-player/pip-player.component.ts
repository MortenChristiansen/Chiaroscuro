import { Component, ElementRef, OnInit, viewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-pip-player',
  template: `
    <div class="pip-container">
      <video
        #videoPlayer
        class="pip-video"
        controls
        autoplay
        (loadedmetadata)="onLoadedMetadata()"
      ></video>
    </div>
  `,
  styles: [
    `
      :host {
        position: fixed;
        inset: 0;
        width: 100vw;
        height: 100vh;
        background: #222;
        border-radius: 0;
        box-shadow: none;
        z-index: 9999;
        display: flex;
        align-items: center;
        justify-content: center;
      }
      .pip-container {
        width: 100vw;
        height: 100vh;
        display: flex;
        align-items: center;
        justify-content: center;
        background: transparent;
        border-radius: 0;
        box-shadow: none;
      }
      .pip-video {
        width: 100vw;
        height: 100vh;
        border-radius: 0;
        background: #000;
        object-fit: contain;
      }
    `,
  ],
})
export default class PipPlayerComponent implements OnInit {
  videoPlayer = viewChild<ElementRef<HTMLVideoElement>>('videoPlayer');
  private streamUrl: string = '';
  private timestamp: number = 0;

  constructor(private route: ActivatedRoute) {}

  ngOnInit(): void {
    this.route.queryParamMap.subscribe((params) => {
      const encodedUrl = params.get('url');
      const timestampStr = params.get('timestamp');
      if (encodedUrl) {
        try {
          this.streamUrl = atob(encodedUrl);
        } catch {
          this.streamUrl = '';
        }
      }
      this.timestamp = timestampStr ? parseFloat(timestampStr) : 0;

      if (this.streamUrl) {
        console.log('PIP Player URL:', this.streamUrl);
        const video = this.videoPlayer()!.nativeElement;
        video.src = this.streamUrl;
        video.currentTime = this.timestamp;
        //video.play();
      } else {
        console.error('Invalid or missing stream URL');
      }
    });
  }

  onLoadedMetadata(): void {
    const video = this.videoPlayer()!.nativeElement;
    if (video.currentTime !== this.timestamp) {
      video.currentTime = this.timestamp;
    }
  }
}
