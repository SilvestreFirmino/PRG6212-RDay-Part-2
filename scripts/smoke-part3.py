"""Exercise MVC -> API -> SQL -> Blob Storage on an isolated demo deployment.
Run: python scripts/smoke-part3.py http://localhost:8080
Creates uniquely named test accounts/events; do not point at a production service.
"""
import base64
import datetime as dt
import html
import http.cookiejar
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid

BASE = (sys.argv[1] if len(sys.argv) > 1 else "http://localhost:8080").rstrip("/")


class Browser:
    def __init__(self):
        self.opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))

    def get(self, path):
        with self.opener.open(BASE + path, timeout=40) as response:
            return response.read().decode(), response.url

    def post(self, page, action, fields, upload=False):
        markup, _ = self.get(page)
        match = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', markup)
        assert match, "Missing anti-forgery token: " + page
        fields = dict(fields, __RequestVerificationToken=html.unescape(match[1]))
        if upload:
            boundary = "RaceDay" + uuid.uuid4().hex
            data = b""
            for key, value in fields.items():
                data += (f'--{boundary}\r\nContent-Disposition: form-data; name="{key}"\r\n\r\n{value}\r\n').encode()
            data += (f'--{boundary}\r\nContent-Disposition: form-data; name="banner"; filename="test.png"\r\nContent-Type: image/png\r\n\r\n').encode()
            data += base64.b64decode("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jR9sAAAAASUVORK5CYII=")
            data += f'\r\n--{boundary}--\r\n'.encode()
            content_type = "multipart/form-data; boundary=" + boundary
        else:
            data = urllib.parse.urlencode(fields).encode()
            content_type = "application/x-www-form-urlencoded"
        request = urllib.request.Request(BASE + action, data, {"Content-Type": content_type})
        with self.opener.open(request, timeout=60) as response:
            return response.read().decode(), response.url


def main():
    organiser, participant, visitor = Browser(), Browser(), Browser()
    for attempt in range(60):
        try:
            page, _ = visitor.get("/Account/Login")
            if "Sign in to RaceDay" in page:
                break
        except (urllib.error.URLError, TimeoutError):
            pass
        time.sleep(2)
    else:
        raise AssertionError("Website did not become ready")
    _, target = visitor.get("/Organiser")
    assert "/Account/Login" in target, "Protected page must redirect"
    suffix = uuid.uuid4().hex[:10]
    for browser, role in [(organiser, "Organiser"), (participant, "Participant")]:
        email = role.lower() + suffix + "@example.co.za"
        fields = dict(FirstName=role, LastName="Smoke", Email=email, Password="Password123!", Role=role,
                      DateOfBirth="2000-01-01", Gender="Female", EmergencyContactName="Contact", EmergencyContactPhone="0712345678")
        page, target = browser.post("/Account/Register", "/Account/Register", fields)
        assert "/Account/Login" in target, "Registration failed: " + page[:500]
        page, target = browser.post("/Account/Login", "/Account/Login", dict(Email=email, Password="Password123!"))
        assert "/Account/Login" not in target, "Login failed"
    page, _ = participant.get("/Organiser")
    assert "This page is for Organiser" in page
    now = dt.datetime.utcnow().replace(microsecond=0)
    dates = lambda days: (now + dt.timedelta(days=days)).isoformat(timespec="minutes")
    name = "Smoke Race " + suffix
    event = dict(EventId="0", Name=name, Description="Integration test event", EventType="Running",
                 StartDateTime=dates(30), EndDateTime=dates(31), VenueName="Test Park", AddressLine1="1 Main Road",
                 City="Johannesburg", Province="Gauteng", TimeZoneId="South Africa Standard Time",
                 RegistrationOpenUtc=dates(-1), RegistrationCloseUtc=dates(20))
    page, target = organiser.post("/Organiser/Create", "/Organiser/Edit", event, upload=True)
    assert name in page and "banner was not uploaded" not in page, "Event/banner creation failed"
    match = re.search(r'/Organiser/Edit/(\d+)', html.unescape(page))
    assert match, "New event link missing"
    event_id = match[1]
    page, _ = organiser.get("/Home/Details/" + event_id)
    assert "event banner" not in page or "banner" in page
    with organiser.opener.open(BASE + "/Home/Banner/" + event_id) as response:
        assert response.read().startswith(b"\x89PNG"), "Azure banner was not returned through API/MVC"
    category_url = "/Organiser/Category?eventId=" + event_id
    page, _ = organiser.post(category_url, "/Organiser/Category", dict(EventId=event_id, CategoryId="0", Name="10 km",
                 DistanceKm="10", EntryFee="100", Capacity="50", IsActive="true"))
    assert "10 km" in page, "Category creation failed"
    organiser.post("/Organiser", "/Organiser/Status", dict(eventId=event_id, status="Published"))
    page, _ = participant.get("/Home/Details/" + event_id)
    category_id = re.search(r'<option value="(\d+)">10 km', page)[1]
    page, _ = participant.post("/Home/Details/" + event_id, "/Participant/Enrol", dict(eventId=event_id, categoryId=category_id, emergencyConsent="true"))
    assert name in page and "Pending" in page, "Entry not visible"
    page, _ = participant.post("/Home/Details/" + event_id, "/Participant/Enrol", dict(eventId=event_id, categoryId=category_id, emergencyConsent="true"))
    assert "already entered" in page, "Duplicate entry not rejected"
    organiser.post("/Organiser", "/Organiser/Status", dict(eventId=event_id, status="Completed"))
    page, _ = organiser.get("/Organiser/Enrolments/" + event_id)
    result_url = html.unescape(re.search(r'href="([^"]*?/Organiser/Result[^\"]*)"', page)[1])
    enrollment_id = urllib.parse.parse_qs(urllib.parse.urlsplit(result_url).query)["enrollmentId"][0]
    page, _ = organiser.post(result_url, "/Organiser/Result", dict(EventId=event_id, EnrollmentId=enrollment_id,
                ResultStatus="Finished", DurationMilliseconds="2700000", OverallPosition="1", CategoryPosition="1"))
    assert "Finished" in page, "Result publishing failed"
    page, _ = participant.get("/Participant/Results")
    assert name in page and "00:45:00" in page, "Participant cannot see result"
    page, _ = participant.post("/Profile", "/Profile", dict(FirstName="Updated", LastName="Runner", PhoneNumber="0712345678"))
    assert 'value="Updated"' in page, "Profile update failed"
    organiser.post("/Organiser", "/Account/Logout", {})
    _, target = organiser.get("/Organiser")
    assert "/Account/Login" in target
    print("PASS: registration, isolated roles, event/banner, categories, enrolment, duplicate rejection, results, profile and logout")


if __name__ == "__main__":
    main()
