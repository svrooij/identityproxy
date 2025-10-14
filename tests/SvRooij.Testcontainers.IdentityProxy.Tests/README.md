# Svrooij.Testcontainers.IdentityProxy.Tests

These tests use a local docker image of the Identity Proxy, make sure it is built and available locally before running the tests.

```bash
cd .\src
docker build -t svrooij/identityproxyapi:test -f .\IdentityProxy.Api\Dockerfile .
```
