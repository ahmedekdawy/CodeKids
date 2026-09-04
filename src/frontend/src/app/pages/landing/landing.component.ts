import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../auth.service';
import { SiteBrandService } from '../../site-brand.service';
import { LanguageSwitcherComponent } from '../../shared/language-switcher/language-switcher.component';
import { ThemeSwitcherComponent } from '../../shared/theme-switcher/theme-switcher.component';
import { SiteBrandComponent } from '../../shared/site-brand/site-brand.component';
import { TranslatePipe } from '../../shared/translate.pipe';

interface LandingCard {
  icon: string;
  titleKey: string;
  textKey: string;
}

interface LandingStat {
  valueKey: string;
  labelKey: string;
}

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, TranslatePipe, LanguageSwitcherComponent, ThemeSwitcherComponent, SiteBrandComponent],
  templateUrl: './landing.component.html',
  styleUrl: './landing.component.css'
})
export class LandingComponent {
  readonly brand = inject(SiteBrandService);
  readonly auth = inject(AuthService);

  readonly stats: LandingStat[] = [
    { valueKey: 'landing.stats.gradesValue', labelKey: 'landing.stats.gradesLabel' },
    { valueKey: 'landing.stats.systemsValue', labelKey: 'landing.stats.systemsLabel' },
    { valueKey: 'landing.stats.shiftsValue', labelKey: 'landing.stats.shiftsLabel' },
    { valueKey: 'landing.stats.followValue', labelKey: 'landing.stats.followLabel' }
  ];

  readonly schoolTypes: LandingCard[] = [
    { icon: '🏫', titleKey: 'landing.types.general', textKey: 'landing.types.generalText' },
    { icon: '🌍', titleKey: 'landing.types.language', textKey: 'landing.types.languageText' },
    { icon: '🕌', titleKey: 'landing.types.azhari', textKey: 'landing.types.azhariText' }
  ];

  readonly services: LandingCard[] = [
    { icon: '🎓', titleKey: 'landing.services.school', textKey: 'landing.services.schoolText' },
    { icon: '🕗', titleKey: 'landing.services.sessions', textKey: 'landing.services.sessionsText' },
    { icon: '🔴', titleKey: 'landing.services.live', textKey: 'landing.services.liveText' },
    { icon: '🎬', titleKey: 'landing.services.recorded', textKey: 'landing.services.recordedText' },
    { icon: '📝', titleKey: 'landing.services.assignments', textKey: 'landing.services.assignmentsText' },
    { icon: '🎯', titleKey: 'landing.services.exams', textKey: 'landing.services.examsText' },
    { icon: '📊', titleKey: 'landing.services.weekly', textKey: 'landing.services.weeklyText' },
    { icon: '💬', titleKey: 'landing.services.chat', textKey: 'landing.services.chatText' },
    { icon: '🔔', titleKey: 'landing.services.notifications', textKey: 'landing.services.notificationsText' },
    { icon: '❓', titleKey: 'landing.services.ask', textKey: 'landing.services.askText' },
    { icon: '🗓️', titleKey: 'landing.services.timetable', textKey: 'landing.services.timetableText' },
    { icon: '✅', titleKey: 'landing.services.attendance', textKey: 'landing.services.attendanceText' }
  ];

  readonly steps: LandingCard[] = [
    { icon: '1', titleKey: 'landing.how.step1', textKey: 'landing.how.step1Text' },
    { icon: '2', titleKey: 'landing.how.step2', textKey: 'landing.how.step2Text' },
    { icon: '3', titleKey: 'landing.how.step3', textKey: 'landing.how.step3Text' },
    { icon: '4', titleKey: 'landing.how.step4', textKey: 'landing.how.step4Text' }
  ];

  readonly teacherPoints = [
    'landing.teachers.point1',
    'landing.teachers.point2',
    'landing.teachers.point3',
    'landing.teachers.point4'
  ];

  readonly parentPoints = [
    'landing.parents.point1',
    'landing.parents.point2',
    'landing.parents.point3',
    'landing.parents.point4'
  ];

  readonly year = new Date().getFullYear();

  readonly contactPhone = '+201141093736';
  readonly contactPhoneDisplay = '+20 114 109 3736';
  readonly whatsAppUrl = `https://wa.me/${this.contactPhone.replace(/\D/g, '')}`;
  readonly telUrl = `tel:${this.contactPhone}`;
}
