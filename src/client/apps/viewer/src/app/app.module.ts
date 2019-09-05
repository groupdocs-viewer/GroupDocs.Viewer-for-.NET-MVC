import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';

import { AppComponent } from './app.component';
import {ViewerModule} from "@groupdocs.examples.angular/viewer";

@NgModule({
  declarations: [AppComponent],
  imports: [BrowserModule,
    ViewerModule.forRoot("http://localhost:8080")],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule {}
