"""Local disposable-container checks for physical deletion and expiry."""
import json
import re
import subprocess
import time
import unittest
import test_api

test_api.BASE = "http://127.0.0.1:5210"
request = test_api.request

def sql(statement):
    result = subprocess.run(["docker", "exec", "case-reviews-visitor-database-1", "psql", "-U", "visitor", "-d", "visitor_demo", "-tAc", statement], capture_output=True, text=True, check=True)
    return result.stdout.strip()

class LifecycleTests(unittest.TestCase):
    def test_reset_and_expiry_delete_records(self):
        for mode in ["reset", "expiry"]:
            status, session = request("/demo/session", "POST")
            self.assertEqual(status, 200)
            subject, token = session["subject"], session["accessToken"]
            self.assertRegex(subject, r"^visitor-[a-f0-9]{32}$")
            status, staff = request("/Staff", "POST", {"firstName":"Synthetic", "lastName":"Lifecycle", "orNumber":"OR1234567", "office":0}, token)
            self.assertEqual(status, 201)
            status, review = request("/Review", "POST", {"program":0,"staffId":staff["id"],"caseNumber":123456789,"reviewDate":"2026-09-18T00:00:00Z","reviewElements":[{"program":0,"reviewedElement":0}]}, token)
            self.assertEqual(status, 201)
            if mode == "reset":
                self.assertEqual(request("/demo/session", "DELETE", token=token)[0], 204)
            else:
                sql(f"""UPDATE "VisitorSessions" SET "ExpiresAt" = NOW() - INTERVAL '1 minute' WHERE "Id" = '{subject}'""")
                self.assertEqual(request("/Staff", token=token)[0], 401)
                subprocess.run(["docker", "restart", "case-reviews-visitor-api-1"], capture_output=True, check=True)
            deadline = time.monotonic() + 30
            while time.monotonic() < deadline:
                counts = [int(sql(f'SELECT COUNT(*) FROM "{table}" WHERE "{field}" = \'{subject}\'')) for table, field in [("VisitorSessions","Id"),("Staff","CreatedBy"),("Reviews","ReviewedBy")]]
                if counts == [0, 0, 0]: break
                time.sleep(0.25)
            self.assertEqual(counts, [0, 0, 0])
            self.assertEqual(request("/Staff", token=token)[0], 401)

if __name__ == "__main__": unittest.main(verbosity=2)
