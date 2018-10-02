# Apple Keychain: migrating netcoreapp2.0 to netcoreapp2.1 fails to read existing secrets

## Synopsis

I use a `netstandard2.0` library that wraps the native keychain APIs in
macOS for secure secret storage and retrieval. It is not possible to migrate
from `netcoreapp2.0` to `netcoreapp2.1` and continue to read keychain items
created via the `netcoreapp2.0` version of the app.

## Test Case

A full test case is available that reproduces this issue on at least macOS
10.13.6 and 10.14:

* Install [.NET Core SDK 2.1.402](https://www.microsoft.com/net/download/thank-you/dotnet-sdk-2.1.402-macos-x64-installer).
* Clone the repository.
* Run `./run-test-case.sh` from the root of the checkout.
* Observe that on the first iteration of the `netcoreapp2.1` version of the
  test app, the secret can no longer be read without explicit authorization.

![Screencast](screencast.gif "Screencast")

## Detailed Description of Issue

Over the last year, with my app targeting `netcoreapp2.0/osx.10.12-x64`,
across hundreds of builds/releases, all builds were able to access secrets
stored from previous builds of the app:

| App Target Framework | App Version | Keychain Operation |
|-|-|-|
| `netcoreapp2.0` | `1`   | _Create New Secret_ (did not previously exist on machine) **✓** |
| `netcoreapp2.0` | `2`   | _Read Existing Secret_ **✓** |
| `netcoreapp2.0` | `3`   | _Read Existing Secret_ **✓** |
| `netcoreapp2.0` | `⋯`   | _Read Existing Secret_ **✓** |
| `netcoreapp2.0` | `150` | _Read Existing Secret_ **✓** |

Then last week, I decided to migrate it to `netcoreapp2.1`. Literally, the
_only_ change to the whole build was updating `<TargetFramework>` in the
project, which resulted in the **Read Existing Secret** operation either
**failing** or displaying a keychain unlock dialog, depending on the context
in which the app was run:

| App Target Framework | App Version | Keychain Operation |
|-|-|-|
| `netcoreapp2.1` | `151` |  _Read Existing Secret_ **→** ❌ _**NEW FAILURE**_

Reverting back to `netcoreapp2.0` results in being able to read the
secret from the keychain again:

| App Target Framework | App Version | Keychain Operation |
|-|-|-|
| `netcoreapp2.0` | `152`   | _Read Existing Secret_ **✓** |

## Important Information to Note

* The app was always a full _published_ and self-contained app, not relying
  on the .NET Core SDK or runtime on the machine. That is, `dotnet` is not
  involved at _runtime_ here.

* The app was always run _from the exact same path_; the access control list
  for keychain items stores both the absolute path of the allowed process
  and presumably metadata from the native binary. With the path never changing
  across all deployed versions on the same machine, the `netcoreapp2.0`
  versions would be authorized to read the secret.

* The app was never signed and it remains unsigned.

* `security dump-keychain -a` indicates _two_ entries in the ACL list for the
  item _after_ first running the `netcoreapp2.1` version:

  ```
  entry 1:
    authorizations (6): decrypt derive export_clear export_wrapped mac sign
      don't-require-password
      description: <service-id-of-secret-redacted>
      applications (2):
        0: /identical/absolute/path/to/app/redacted (status -2147415734)
        1: /identical/absolute/path/to/app/redacted (OK)
  ```

  So there are now _two_ ACL entries with the same path to binary, but
  two separate access statuses. The `-2147415734` (`0x8001094a`) status
  is new after running the `netcoreapp2.1` version, and indicates the
  failure, which seems to be `CSSMERR_CSP_VERIFY_FAILED`.

  [This technical note from Apple seems to relate to Xcode, but this might
  be a clue](https://developer.apple.com/library/archive/technotes/tn2318/_index.html):

  > For any of the following errors:
  >
  > * Error: 0x8001094A -2147415734 CSSMERR_CSP_VERIFY_FAILED
  > * CSSM_SignData returned: 8001094A
  >
  > If OCSP and CRL checking are temporarily turned off (in Keychain
    Access > Preferences > Certificates) and this resolves the Xcode
    build, reinstall Xcode (to restore its own signature) and also
    ensure there are no network connectivity issues on the Mac that
    is running Xcode.

* I believe this must have something to do with changes to native code
  generation from `netcoreapp2.0` to `netcoreapp2.1`.