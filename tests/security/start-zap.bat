@echo off
rem Starts OWASP ZAP with Java 23. ZAP needs Java 17+, but JAVA_HOME on this PC points to Java 8 (used by JMeter).
cd /d "C:\Program Files\ZAP\Zed Attack Proxy"
start "" "C:\Users\idppl\.jdks\openjdk-23.0.2\bin\javaw.exe" -Xmx2g -jar zap-2.17.0.jar
