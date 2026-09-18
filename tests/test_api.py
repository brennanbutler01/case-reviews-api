"""HTTP regressions against the isolated Compose database; synthetic records only."""
import json
import unittest
import uuid
from urllib.request import Request, urlopen
from urllib.error import HTTPError

BASE = "http://127.0.0.1:5190"

def request(path, method="GET", body=None, token=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = "Bearer " + token
    data = None if body is None else json.dumps(body).encode()
    try:
        response = urlopen(Request(BASE + path, data=data, headers=headers, method=method), timeout=10)
    except HTTPError as error:
        response = error
    with response:
        raw = response.read()
        try:
            result = json.loads(raw)
        except ValueError:
            result = raw.decode()
        return response.status, result

class ApiOwnershipTests(unittest.TestCase):
    def setUp(self):
        self.alice = request("/dev/token/alice")[1]["accessToken"]
        self.bob = request("/dev/token/bob")[1]["accessToken"]
        self.staff = []
        self.reviews = []

    def tearDown(self):
        for review, token in self.reviews:
            request("/Review/" + review["id"], "DELETE", token=token)
        for staff, token in self.staff:
            request("/Staff/" + staff["id"], "DELETE", token=token)

    def create_staff(self, token):
        status, staff = request("/Staff", "POST", {"firstName":"Synthetic", "lastName":"Tester",
            "orNumber":"OR1234567", "office":0, "createdBy":"forged-owner"}, token)
        self.assertEqual(status, 201, staff)
        self.staff.append((staff, token))
        return staff

    def create_review(self, token, staff):
        body = {"reviewedBy":"forged-owner", "program":0, "staffId":staff["id"],
            "caseNumber":123456789, "reviewDate":"2026-09-17T00:00:00Z", "reportingSystem":0,
            "reviewElements":[{"program":0,"reviewedElement":0,"comments":"synthetic"}],
            "magiEligibles":{"adultsEligibleActual":1}}
        status, review = request("/Review", "POST", body, token)
        self.assertEqual(status, 201, review)
        self.reviews.append((review, token))
        return review

    def test_staff_ownership_and_route_ids(self):
        staff = self.create_staff(self.alice)
        self.assertEqual(staff["createdBy"], "demo-alice")
        path = "/Staff/" + staff["id"]
        for method in ["GET", "PUT", "DELETE"]:
            self.assertEqual(request(path, method, staff if method == "PUT" else None, self.bob)[0],404)
        forged = {**staff,"id":str(uuid.uuid4())}
        self.assertEqual(request(path,"PUT",forged,self.alice)[0],400)
        changed = {**staff,"lastName":"Updated","createdBy":"demo-bob"}
        status, result = request(path,"PUT",changed,self.alice)
        self.assertEqual(status,200)
        self.assertEqual(result["createdBy"],"demo-alice")
        self.assertEqual(result["lastName"],"Updated")
        self.assertNotIn(staff["id"],[s["id"] for s in request("/Staff",token=self.bob)[1]])

    def test_review_ownership_children_update_and_staff_delete(self):
        staff = self.create_staff(self.alice)
        review = self.create_review(self.alice,staff)
        self.assertEqual(review["reviewedBy"],"demo-alice")
        path = "/Review/" + review["id"]
        for method in ["GET","PUT","DELETE"]:
            self.assertEqual(request(path,method,review if method == "PUT" else None,self.bob)[0],404)
        self.assertEqual(request("/Staff/"+staff["id"],"DELETE",token=self.alice)[0],409)
        new = {**review,"reviewedBy":"demo-bob","reviewElements":[
            {"program":0,"reviewedElement":1,"comments":"replacement","isReviewed":True}],
            "magiEligibles":None}
        status, updated = request(path,"PUT",new,self.alice)
        self.assertEqual(status,200,updated)
        persisted = request(path,token=self.alice)[1]
        self.assertEqual(persisted["reviewedBy"],"demo-alice")
        self.assertEqual(len(persisted["reviewElements"]),1)
        self.assertEqual(persisted["reviewElements"][0]["reviewedElement"],1)
        self.assertEqual(persisted["reviewElements"][0]["comments"],"replacement")
        self.assertIsNone(persisted["magiEligibles"])
        self.assertNotIn(review["id"],[r["id"] for r in request("/Review",token=self.bob)[1]])
        for count in [2, 3]:
            status, result = request(path, "PUT", {**new, "magiEligibles": {"adultsEligibleActual": count}}, self.alice)
            self.assertEqual(status, 200, result)
            self.assertEqual(request(path, token=self.alice)[1]["magiEligibles"]["adultsEligibleActual"], count)
        self.assertEqual(request(path, "PUT", {**new, "reviewElements": [None]}, self.alice)[0], 400)
        bob_staff = self.create_staff(self.bob)
        self.assertEqual(request(path,"PUT",{**new,"staffId":bob_staff["id"]},self.alice)[0],400)
        self.assertEqual(request("/Review","POST",{**new,"staffId":bob_staff["id"]},self.alice)[0],400)
        self.assertEqual(request(path,"PUT",{**new,"program":999},self.alice)[0],400)
        self.assertEqual(request(path,"PUT",{**new,"id":str(uuid.uuid4())},self.alice)[0],400)
        self.assertEqual(request(path,"DELETE",token=self.alice)[0],200)
        self.assertEqual(request(path,token=self.alice)[0],404)

    def test_anonymous_requests_are_rejected(self):
        for path in ["/Staff","/Review"]:
            for method in ["GET","POST","PUT","DELETE"]:
                route = path if method in ["GET","POST"] else path+"/"+str(uuid.uuid4())
                self.assertEqual(request(route,method,{} if method in ["POST","PUT"] else None)[0],401)
        self.assertEqual(request("/dev/token/mallory")[0],404)
        self.assertEqual(request("/Cleanup","POST",{},self.alice)[0],404)

if __name__ == "__main__":
    unittest.main(verbosity=2)
