import { Component, OnInit, OnDestroy, HostListener, HostBinding, inject, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslationService, TranslatePipe, LanguageSwitcherComponent } from '../../core/i18n';
import { AuthService } from '../../core/services/services';
import { UserRole } from '../../core/models';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, TranslatePipe, LanguageSwitcherComponent],
  templateUrl: './landing.component.html',
  styleUrls: ['./landing.component.scss']
})
export class LandingComponent implements OnInit, OnDestroy {
  i18n = inject(TranslationService);
  auth = inject(AuthService);

  @HostBinding('attr.dir')  get dir()  { return this.i18n.dir(); }
  @HostBinding('attr.lang') get lang() { return this.i18n.locale(); }

  scrolled      = false;
  currentYear   = new Date().getFullYear();
  activeFeature = 0;
  countsDone    = false;
  private rafId: number | null = null;
  private observer: IntersectionObserver | null = null;

  trustItems = ['landing.trust1', 'landing.trust2', 'landing.trust3', 'landing.trust4', 'landing.trust5'];

  stats = [
    { value: 160,  suffix: '',  labelKey: 'landing.statTemplates',     icon: '🖨️' },
    { value: 6,    suffix: '',  labelKey: 'landing.statLanguages',      icon: '🌍' },
    { value: 4,    suffix: '',  labelKey: 'landing.statScoringMethods', icon: '📊' },
    { value: 99.9, suffix: '%', labelKey: 'landing.statUptime',         icon: '⚡' },
  ];

  displayStats = this.stats.map(s => ({ ...s, display: '0' }));

  features = [
    { icon: '🏁', tagKey: 'landing.feat1Tag', titleKey: 'landing.feat1Title', descKey: 'landing.feat1Desc', detailKey: 'landing.feat1Detail', color: '#1E3A5F', accent: '#4A90D9' },
    { icon: '🏆', tagKey: 'landing.feat2Tag', titleKey: 'landing.feat2Title', descKey: 'landing.feat2Desc', detailKey: 'landing.feat2Detail', color: '#2D6A4F', accent: '#52B788' },
    { icon: '🖨️', tagKey: 'landing.feat3Tag', titleKey: 'landing.feat3Title', descKey: 'landing.feat3Desc', detailKey: 'landing.feat3Detail', color: '#7B2D8B', accent: '#C084FC' },
    { icon: '🌍', tagKey: 'landing.feat4Tag', titleKey: 'landing.feat4Title', descKey: 'landing.feat4Desc', detailKey: 'landing.feat4Detail', color: '#92400E', accent: '#FCD34D' },
    { icon: '🔗', tagKey: 'landing.feat5Tag', titleKey: 'landing.feat5Title', descKey: 'landing.feat5Desc', detailKey: 'landing.feat5Detail', color: '#0E4D6E', accent: '#38BDF8' },
    { icon: '🏅', tagKey: 'landing.feat6Tag', titleKey: 'landing.feat6Title', descKey: 'landing.feat6Desc', detailKey: 'landing.feat6Detail', color: '#7C3141', accent: '#F87171' },
  ];

  steps = [
    { num: '01', titleKey: 'landing.step1Title', descKey: 'landing.step1Desc', icon: '🏠' },
    { num: '02', titleKey: 'landing.step2Title', descKey: 'landing.step2Desc', icon: '✅' },
    { num: '03', titleKey: 'landing.step3Title', descKey: 'landing.step3Desc', icon: '🔑' },
    { num: '04', titleKey: 'landing.step4Title', descKey: 'landing.step4Desc', icon: '📊' },
  ];

  syncItems = [
    { icon: '🏁', titleKey: 'landing.sync1Title', descKey: 'landing.sync1Desc' },
    { icon: '⚡', titleKey: 'landing.sync2Title', descKey: 'landing.sync2Desc' },
    { icon: '🕊️', titleKey: 'landing.sync3Title', descKey: 'landing.sync3Desc' },
    { icon: '🏠', titleKey: 'landing.sync4Title', descKey: 'landing.sync4Desc' },
    { icon: '⭐', titleKey: 'landing.sync5Title', descKey: 'landing.sync5Desc' },
    { icon: '🖨️', titleKey: 'landing.sync6Title', descKey: 'landing.sync6Desc' },
  ];

  phases = [
    { num: '01', icon: '🏠', titleKey: 'landing.phase1Title', descKey: 'landing.phase1Desc' },
    { num: '02', icon: '🏁', titleKey: 'landing.phase2Title', descKey: 'landing.phase2Desc' },
    { num: '03', icon: '📊', titleKey: 'landing.phase3Title', descKey: 'landing.phase3Desc' },
    { num: '04', icon: '🏆', titleKey: 'landing.phase4Title', descKey: 'landing.phase4Desc' },
  ];

  distances = [
    { icon: '🚀', labelKey: 'landing.distSprint',  rangeKey: 'landing.distSprintRange'  },
    { icon: '🏁', labelKey: 'landing.distMiddle',  rangeKey: 'landing.distMiddleRange'  },
    { icon: '🌄', labelKey: 'landing.distLong',    rangeKey: 'landing.distLongRange'    },
    { icon: '🌍', labelKey: 'landing.distExtreme', rangeKey: 'landing.distExtremeRange' },
  ];

  isLoggedIn = computed(() => !!this.auth.currentUser());

  dashboardRoute = computed(() => {
    const user = this.auth.currentUser();
    if (!user) return '/auth/login';
    const routes: Record<number, string> = {
      [UserRole.SuperAdmin]:     '/admin/dashboard',
      [UserRole.CountryManager]: '/country/dashboard',
      [UserRole.ClubManager]:    '/club/dashboard',
      [UserRole.Fancier]:        '/fancier/dashboard',
    };
    return routes[user.role] ?? '/auth/login';
  });

  ngOnInit() {
    this.setupIntersectionObserver();
  }

  ngOnDestroy() {
    if (this.observer) this.observer.disconnect();
    if (this.rafId)    cancelAnimationFrame(this.rafId);
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
    const start    = performance.now();

    const tick = (now: number) => {
      const elapsed  = now - start;
      const progress = Math.min(elapsed / duration, 1);
      const ease     = 1 - Math.pow(1 - progress, 3);

      this.displayStats = this.stats.map(s => ({
        ...s,
        display: s.value % 1 === 0
          ? Math.round(s.value * ease).toString()
          : (s.value * ease).toFixed(1)
      }));

      if (progress < 1) this.rafId = requestAnimationFrame(tick);
    };

    this.rafId = requestAnimationFrame(tick);
  }
}
