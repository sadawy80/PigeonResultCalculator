import { Component, OnInit, OnDestroy, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.scss']
})
export class LandingComponent implements OnInit, OnDestroy {

  scrolled = false;
  currentYear = new Date().getFullYear();
  activeFeature = 0;
  countsDone = false;
  private rafId: number | null = null;
  private observer: IntersectionObserver | null = null;

  stats = [
    { value: 160,  suffix: '',  label: 'Print Templates',    icon: '🖨️' },
    { value: 6,    suffix: '',  label: 'Languages',          icon: '🌍' },
    { value: 4,    suffix: '',  label: 'Scoring Methods',    icon: '📊' },
    { value: 99.9, suffix: '%', label: 'Uptime SLA',         icon: '⚡' },
  ];

  displayStats = this.stats.map(s => ({ ...s, display: '0' }));

  features = [
    {
      icon: '🏁',
      tag: 'RACE MANAGEMENT',
      title: 'ETS File Ingestion',
      desc: 'Upload race files and watch results rank in real time. Velocity calculated to 4 decimal places. Live SignalR push to every connected screen.',
      detail: 'Supports all major ETS clock formats. Auto-detects flight duration, computes m/min velocity, flags duplicates and invalid timestamps before any result is stored.',
      color: '#1E3A5F',
      accent: '#4A90D9'
    },
    {
      icon: '🏆',
      tag: 'PROGRAMME ENGINE',
      title: 'Ace Pigeon · Super Ace · Best Loft',
      desc: 'Four scoring methods. Three aggregate result types. Configurable qualification rules. One calculate button.',
      detail: 'Average Velocity · Points by Rank · Velocity % · Total Velocity. Super Ace supports All Races Required, Minimum Count, or Minimum Percentage qualification.',
      color: '#2D6A4F',
      accent: '#52B788'
    },
    {
      icon: '🖨️',
      tag: 'PRINT & PDF',
      title: '160 Professional Templates',
      desc: 'Race results, Best Loft, Ace Pigeon, Super Ace, and 50 award certificates. Print directly from your browser — no dependencies.',
      detail: 'Navy · Gold · Dark Mode · Minimal · Sporty · Heritage · Branded. A4 portrait and landscape. Arabic outputs with full RTL layout. Print dialog = PDF.',
      color: '#7B2D8B',
      accent: '#C084FC'
    },
    {
      icon: '🌍',
      tag: 'MULTILINGUAL',
      title: '6 Languages · RTL Support',
      desc: 'English · Français · Belgisch (Vlaams) · العربية · 中文 · Español. Switch at runtime — one build serves all.',
      detail: 'Signal-based runtime loader. 260 translation keys across 10 sections. Arabic inverts sidebar, mirrors tables, preserves number direction. Chinese gets proper font stack.',
      color: '#92400E',
      accent: '#FCD34D'
    },
    {
      icon: '🔗',
      tag: 'INTEGRATIONS',
      title: 'PigeonLoftManager.com Link',
      desc: 'Fanciers link their loft account with one click. Club manager reviews and approves. Access token issued. Data flows automatically.',
      detail: 'LinkToken verification · HMAC-SHA256 webhook signatures · access token instantly nullified on revoke · 18 REST endpoints covering the full PLM ↔ PRC lifecycle.',
      color: '#0E4D6E',
      accent: '#38BDF8'
    },
    {
      icon: '📡',
      tag: 'OBSERVABILITY',
      title: 'Grafana · Prometheus · Loki',
      desc: '7 auto-provisioned dashboards. 30+ alert rules. Business metrics alongside infrastructure. Slack + PagerDuty routing.',
      detail: 'Platform Overview · API Performance · Business Metrics · Infrastructure · Database & Cache · Logs Explorer · Alerts Overview. SQL Server, Redis, node-exporter, cAdvisor.',
      color: '#7C3141',
      accent: '#F87171'
    }
  ];

  steps = [
    { num: '01', title: 'Fancier requests link', desc: 'On PigeonLoftManager.com, fancier selects their club and clicks "Link to PigeonResultCalculator". A link request is sent server-to-server.', icon: '🏠' },
    { num: '02', title: 'Club manager reviews', desc: 'Club manager sees the pending request in the Integrations panel — loft name, external ID, matched PRC account. Approve or reject with one click.', icon: '✅' },
    { num: '03', title: 'Access token issued', desc: 'On approval, a unique access token is generated and sent to PLM via a signed webhook callback. The link token confirms authenticity.', icon: '🔑' },
    { num: '04', title: 'Live data flows', desc: 'PLM polls the data endpoints with the access token. Results, Ace Pigeon, Super Ace, Best Loft and a full summary appear on the loft profile.', icon: '📡' },
  ];

  stack = [
    { name: 'ASP.NET Core 9',   role: 'REST API + SignalR',       color: '#512BD4' },
    { name: 'Angular 18',       role: 'SPA · Signals · i18n',     color: '#DD0031' },
    { name: 'SQL Server 2022',  role: 'Primary database',         color: '#CC2927' },
    { name: 'Redis 7',          role: 'Cache + pub/sub',          color: '#DC382D' },
    { name: 'Entity Framework', role: 'ORM · Clean Architecture', color: '#512BD4' },
    { name: 'MediatR CQRS',     role: 'Command/Query separation', color: '#00897B' },
    { name: 'Prometheus',       role: 'Metrics + alerting',       color: '#E6522C' },
    { name: 'Grafana 10',       role: '7 dashboards auto-wired',  color: '#F46800' },
    { name: 'Docker Compose',   role: 'One-command deployment',   color: '#2496ED' },
    { name: 'Loki + Promtail',  role: 'Log aggregation',          color: '#F46800' },
    { name: 'JWT + Refresh',    role: 'Auth · 4 role types',      color: '#1E3A5F' },
    { name: 'SignalR',          role: 'Live race tracking',       color: '#512BD4' },
  ];

  ngOnInit() {
    this.setupIntersectionObserver();
  }

  ngOnDestroy() {
    if (this.observer) this.observer.disconnect();
    if (this.rafId) cancelAnimationFrame(this.rafId);
  }

  @HostListener('window:scroll')
  onScroll() {
    this.scrolled = window.scrollY > 60;
  }

  setActiveFeature(i: number) {
    this.activeFeature = i;
  }

  private setupIntersectionObserver() {
    this.observer = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          entry.target.classList.add('visible');
          if (entry.target.classList.contains('stats-trigger') && !this.countsDone) {
            this.countsDone = true;
            this.animateCounts();
          }
        }
      });
    }, { threshold: 0.15 });

    setTimeout(() => {
      document.querySelectorAll('.reveal, .stats-trigger').forEach(el => {
        this.observer?.observe(el);
      });
    }, 100);
  }

  private animateCounts() {
    const duration = 1800;
    const start = performance.now();

    const tick = (now: number) => {
      const elapsed = now - start;
      const progress = Math.min(elapsed / duration, 1);
      const ease = 1 - Math.pow(1 - progress, 3);

      this.displayStats = this.stats.map(s => ({
        ...s,
        display: s.value % 1 === 0
          ? Math.round(s.value * ease).toString()
          : (s.value * ease).toFixed(1)
      }));

      if (progress < 1) {
        this.rafId = requestAnimationFrame(tick);
      }
    };

    this.rafId = requestAnimationFrame(tick);
  }
}
