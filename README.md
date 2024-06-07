Creates an SMTP message out of an HTTP request.

Particularly useful for Nintex workflows when you want to send an email through a different SMTP server other than the configured in Sharepoint.
Or any other app that can send web requests, but cannot send email.
This can run as a Windows service in a server, or modified to run under IIS. Then point your web request to this code, and it will be the relay to your mail server.

