"""Exercise production visitor sessions and real PostgreSQL ownership."""
import os
import unittest
import test_api

test_api.BASE = os.environ.get("VISITOR_API_URL", "http://127.0.0.1:5210")
request = test_api.request

class VisitorTests(test_api.ApiOwnershipTests):
    def setUp(self):
        first_status, first = request("/demo/session", "POST")
        second_status, second = request("/demo/session", "POST")
        self.assertEqual(first_status, 200)
        self.assertEqual(second_status, 200)
        self.alice, self.bob = first["accessToken"], second["accessToken"]
        self.alice_subject, self.bob_subject = first["subject"], second["subject"]
        self.assertNotEqual(self.alice_subject, self.bob_subject)
        self.staff, self.reviews = [], []

    def tearDown(self):
        request("/demo/session", "DELETE", token=self.alice)
        request("/demo/session", "DELETE", token=self.bob)

    def test_reset_revokes_token_and_preserves_other_visitor(self):
        staff = self.create_staff(self.alice)
        self.create_review(self.alice, staff)
        other_staff = self.create_staff(self.bob)
        self.assertEqual(request("/demo/session", "DELETE", token=self.alice)[0], 204)
        self.assertEqual(request("/Staff", token=self.alice)[0], 401)
        self.assertEqual(request("/demo/session", token=self.alice)[0], 401)
        self.assertEqual(request("/Staff/" + other_staff["id"], token=self.bob)[0], 200)

    def test_public_local_tokens_are_unavailable(self):
        self.assertEqual(request("/dev/token/alice")[0], 404)
        self.assertEqual(request("/demo/session", token="invalid-token")[0], 401)

if __name__ == "__main__":
    unittest.main(verbosity=2)
