//using IdentityModel;
//using Liquid;
//using Liquid.Base;
//using Liquid.Base;
using Liquid.Domain;
//using Liquid.OnAzure;
//using Liquid.Platform;
//using Liquid.Runtime;
//using Microservice.Config;
//using Microservice.Models;
//using Microservice.ViewModels;
//using Microsoft.Graph;
//using Microsoft.Identity.Client;
//using Microsoft.IdentityModel.Protocols;
//using Microsoft.IdentityModel.Protocols.OpenIdConnect;
//using Microsoft.IdentityModel.Tokens;
//using System;
//using System.Collections.Generic;
//using System.IdentityModel.Tokens.Jwt;
//using System.Linq;
//using System.Net.Http.Headers;
//using System.Security.Claims;
//using System.Security.Cryptography.X509Certificates;
//using System.Threading.Tasks;

namespace Microservice.Services
{
    internal class WebAuthNService : LightService
    {
        #region WebAuthN (commented)

        //private static readonly List<string> supportedPublicKeyAlgorithms = new() { "-7", "-257" };
        //public static string GenerateNewChallenge() => Convert.ToBase64String(CryptoRandom.CreateRandomKey(24));

        //public async Task<DomainResponse> StartRegisteringWebAuthN(int?   
        //{
        //    Account current = FactoryFromClaims(Context.User);

        //    var profilesQueried = Repository.Get<Profile>(p => p.Accounts[0].Id == current.Id);
        //    var profile = profilesQueried.AsEnumerable().FirstOrDefault();

        //    if (profile is null)
        //        return NoContent();

        //    var account = profile.Accounts.FirstOrDefault(a => a.Id == current.Id);

        //    if (account is null)
        //        return BusinessError("USER_HAS_NO_ACCOUNT");


        //    var newChallenge = GenerateNewChallenge();
        //    account.Credentials.WebAuthNChallenge = newChallenge;

        //    try 
        //    {
        //       await Repository.UpdateAsync(profile);
        //    }
        //    catch (OptimisticConcurrencyLightException)
        //    {
        //        if (tryNum <= 3)
        //            return await StartRegisteringWebAuthN(++tryNum);
        //        else
        //            throw;
        //    }

        //    return Response(new WebAuthNCredentialCreationVM
        //    {
        //        Challenge = newChallenge,
        //        RP = new WebAuthNRelyingPartyVM 
        //        {
        //            Id = WorkBench.IsDevelopmentEnvironment ? "localhost" : config.JWTSelfIssuedAudience,
        //            Name = "Your Company"
        //        },
        //        User = new WebAuthNUserVM
        //        {
        //            Id = account.Id,
        //            Name = profile.Channels.Email,
        //            DisplayName = profile.Name
        //        },
        //        PubKeyCredParams = new List<WebAuthNPublicKeyParamsVM>
        //        {
        //            new WebAuthNPublicKeyParamsVM 
        //            {
        //                Type = "public-key",
        //                Alg = -7
        //            },
        //            new WebAuthNPublicKeyParamsVM
        //            {
        //                Type = "public-key",
        //                Alg = -257
        //            }
        //        },
        //        ExcludeCredentials = new List<WebAuthNCredentialsVM>
        //        (
        //            account.Credentials.WebAuthN.Select(c => new WebAuthNCredentialsVM
        //            {
        //                Id = c.CredentialId,
        //                Type = "public-key"
        //            })
        //        ),
        //        Attestation = "none"
        //    });
        //}

        //public async Task<DomainResponse> AuthenticateByWebAuthN(WebAuthNRequestVM webAuthNRequest, int? tryNum = 1)
        //{
        //    Telemetry.TrackEvent("Authenticate by WebAuthN");

        //    var credentialId = HttpUtility.HtmlDecode(webAuthNRequest.CredentialId);

        ////TODO: Fix the query when activate the code
        //var profilesQueried = Repository.Get<Profile>(
        //    p => p.Accounts.Where(
        //        a => a.Credentials.WebAuthN.Where(
        //            c => c.CredentialId == credentialId
        //        ).ToList()[0] is not null
        //    ).ToList()[0] is not null
        //);
        //var profile = profilesQueried.AsEnumerable().FirstOrDefault();

        //if (profile is null)
        //    return NoContent();

        //var account = profile.Accounts.FirstOrDefault(a => a.Credentials.WebAuthN.Exists(c => c.CredentialId == credentialId));
        //var credential = account.Credentials.WebAuthN.FirstOrDefault(c => c.CredentialId == credentialId);

        //var activeChallenge = account.Credentials.WebAuthNChallenge;

        //if (string.IsNullOrEmpty(activeChallenge))
        //    return BusinessError("CREDENTIAL_DOES_NOT_HAVE_ACTIVE_CHALLENGE");

        //if (!supportedPublicKeyAlgorithms.Contains(credential.Algorithm))
        //{
        //    var algs = string.Join(", ", supportedPublicKeyAlgorithms);
        //    throw new LightException($"User's saved public key algorithm {credential.Algorithm} is not supported. Change to one of [ {algs} ] instead");
        //}

        //var decodedClientDataJSON = Base64Url.Decode(HttpUtility.HtmlDecode(webAuthNRequest.Response.ClientDataJSON));
        //var clientDataJSON = JToken.Parse(Encoding.UTF8.GetString(decodedClientDataJSON)).ToObject<ClientDataJSON>();

        //// TODO: this should not use contains
        //if ((WorkBench.IsDevelopmentEnvironment && !clientDataJSON.Origin.Contains("localhost"))
        //    || (!WorkBench.IsDevelopmentEnvironment && !clientDataJSON.Origin.Contains(config.JWTSelfIssuedAudience)))
        //    return BusinessError("INVALID_ORIGIN");

        //if (clientDataJSON.Type != "webauthn.get")
        //    return BusinessError("INVALID_REQUEST_TYPE");

        //if (Encoding.UTF8.GetString(Base64Url.Decode(clientDataJSON.Challenge)) != activeChallenge)
        //    return BusinessError("INVALID_CHALLENGE");

        //var authenticatorData = Base64Url.Decode(HttpUtility.HtmlDecode(webAuthNRequest.Response.AuthenticatorData));
        //var authenticatorDataEnumerable = authenticatorData.AsEnumerable();

        //var incomingRpIdHash = authenticatorDataEnumerable.Take(32);
        //authenticatorDataEnumerable = authenticatorDataEnumerable.Skip(32);

        //var expectedRp = WorkBench.IsDevelopmentEnvironment ? "localhost" : config.JWTSelfIssuedAudience;

        //using var hasher = new SHA256Managed();
        //var rpIdHash = hasher.ComputeHash(Encoding.UTF8.GetBytes(expectedRp));
        //if (!incomingRpIdHash.SequenceEqual(rpIdHash))
        //{
        //    AddBusinessError("INVALID_RPID");
        //    return Response();
        //}

        //var flags = new BitArray(authenticatorDataEnumerable.Take(1).ToArray());
        //authenticatorDataEnumerable = authenticatorDataEnumerable.Skip(1);

        //var userPresent = flags[0];
        //// Bit 1 reserved for future use (RFU1)
        //var userVerified = flags[2]; // (UV)
        //// Bits 3-5 reserved for future use (RFU2)
        //var attestedCredentialData = flags[6]; // (AT) "Indicates whether the authenticator added attested credential data"
        //var extensionDataIncluded = flags[7]; // (ED)

        //var counter = BitConverter.ToUInt32(authenticatorDataEnumerable.Take(4).ToArray());
        //authenticatorDataEnumerable = authenticatorDataEnumerable.Skip(4);

        //if (counter <= credential.Counter)
        //{
        //    AddBusinessError("INVALID_COUNTER");
        //    return Response();
        //}

        //var hashClientDataJSON = hasher.ComputeHash(decodedClientDataJSON);

        //var sigBase = new byte[authenticatorData.Length + hashClientDataJSON.Length];

        //authenticatorData.CopyTo(sigBase, 0);
        //hashClientDataJSON.CopyTo(sigBase, authenticatorData.Length);

        //var signature = Base64Url.Decode(HttpUtility.HtmlDecode(webAuthNRequest.Response.Signature));

        //var key = credential.PublicKey;
        //if (credential.Algorithm == "-257")
        //{
        //    var rs256key = JsonConvert.DeserializeObject<WebAuthNPublicKeyRS256>(key);
        //    using var rsa = new RSACryptoServiceProvider();
        //    var rsaParameters = new RSAParameters
        //    {
        //        Modulus = Base64Url.Decode(rs256key.N),
        //        Exponent = Base64Url.Decode(rs256key.E)
        //    };
        //    rsa.ImportParameters(rsaParameters);

        //    var rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
        //    rsaDeformatter.SetHashAlgorithm("SHA256");

        //    if (!rsaDeformatter.VerifySignature(hasher.ComputeHash(sigBase), signature))
        //    {
        //        AddBusinessError("INVALID_SIGNATURE");
        //        return Response();
        //    }
        //}

        //if (credential.Algorithm == "-7")
        //{
        //    // https://github.com/scottbrady91/Fido2-Poc/blob/master/ScottBrady91.Fido2.Poc/Controllers/AccountController.cs#L255
        //    var es256key = JsonConvert.DeserializeObject<WebAuthNPublicKeyES256>(key);
        //    using var ecDsa = ECDsa.Create(new ECParameters
        //    {
        //        Curve = ECCurve.NamedCurves.nistP256,
        //        Q = new ECPoint
        //        {
        //            X = Base64Url.Decode(es256key.X),
        //            Y = Base64Url.Decode(es256key.Y)
        //        }
        //    });

        //    if (!ecDsa.VerifyData(sigBase, DeserializeSignature(signature), HashAlgorithmName.SHA256))
        //    {
        //        AddBusinessError("INVALID_SIGNATURE");
        //        return Response();
        //    }
        //}

        //account.Credentials.WebAuthNChallenge = null;

        //credential.Counter = counter;

        //    try 
        //    {
        //       await Repository.UpdateAsync(profile);
        //    }
        //    catch (OptimisticConcurrencyLightException)
        //    {
        //        if (tryNum <= 3)
        //            return await AuthenticateByWebAuthN(webAuthNRequest, ++tryNum);
        //        else
        //            throw;
        //    }

        //var now = WorkBench.UtcNow;
        //var expiration = now.AddDays(AuthenticationService.JWT_VALIDITY_IN_DAYS);

        //var token = GenerateNewJWT(account.Id, profile, now, expiration);
        //return Response(new TokenVM
        //{
        //    IssuedTo = account.Id,
        //    Token = token
        //});
        //}

        //public async Task<DomainResponse> StartAuthenticationWebAuthN(string deviceId, int? tryNum = 1)
        //{

        ////TODO: Fix the query when activate the code
        //var profilesQueried = Repository.Get<Profile>(
        //    p => p.Accounts.Where(
        //        a => a.Credentials.WebAuthN.Where(
        //            c => c.DeviceId == deviceId
        //        ).ToList()[0] is not null
        //    ).ToList()[0] is not null
        //);
        //var profile = profilesQueried.AsEnumerable().FirstOrDefault();

        //if (profile is null)
        //    return NoContent();


        //var account = profile.Accounts.FirstOrDefault(a => a.Credentials.WebAuthN.Exists(c => c.DeviceId == deviceId));

        //var newChallenge = GenerateNewChallenge();
        //account.Credentials.WebAuthNChallenge = newChallenge;

        //    try 
        //    {
        //       await Repository.UpdateAsync(profile);
        //    }
        //    catch (OptimisticConcurrencyLightException)
        //    {
        //        if (tryNum <= 3)
        //            return await StartAuthenticationWebAuthN(deviceId, ++tryNum);
        //        else
        //            throw;
        //    }

        //return Response(new WebAuthNCredentialRequestVM
        //{
        //    Challenge = newChallenge,
        //    RpId = WorkBench.IsDevelopmentEnvironment ? "localhost" : config.JWTSelfIssuedAudience,
        //    AllowCredentials = new()(
        //        account.Credentials.WebAuthN.Select(c => new WebAuthNCredentialsVM
        //        {
        //            Id = c.CredentialId,
        //            Type = "public-key",
        //            Transports = new() { "internal" }
        //        })
        //    ),
        //    UserVerification = "required"
        //});
        //}

        //public async Task<DomainResponse> RegisterWebAuthNDevice(string deviceId, WebAuthNRequestVM webAuthNRequest, int? tryNum = 1)
        //{
        //    Account current = FactoryFromClaims(Context.User);

        //    var profilesQueried = Repository.Get<Profile>(p => p.Accounts[0].Id == current.Id);
        //    var profile = profilesQueried.AsEnumerable().FirstOrDefault();

        //    if (profile is null)
        //        return NoContent();


        //    var account = profile.Accounts.FirstOrDefault(a => a.Id == current.Id);

        //    if (account is null)
        //        return BusinessError("USER_HAS_NO_ACCOUNT");

        //    var activeChallenge = account.Credentials.WebAuthNChallenge;

        //    if (string.IsNullOrEmpty(activeChallenge))
        //        return BusinessError("CREDENTIAL_DOES_NOT_HAVE_ACTIVE_CHALLENGE");

        //    var decodedClientDataJSON = Encoding.UTF8.GetString(Base64Url.Decode(HttpUtility.HtmlDecode(webAuthNRequest.Response.ClientDataJSON)));
        //    var clientDataJSON = JToken.Parse(decodedClientDataJSON).ToObject<ClientDataJSON>();

        //// TODO: this should not use contains
        //    if ((WorkBench.IsDevelopmentEnvironment && !clientDataJSON.Origin.Contains("localhost"))
        //        || (!WorkBench.IsDevelopmentEnvironment && !clientDataJSON.Origin.Contains(config.JWTSelfIssuedAudience)))
        //        return BusinessError("INVALID_ORIGIN");

        //    if (clientDataJSON.Type != "webauthn.create")
        //        return BusinessError("INVALID_REQUEST_TYPE");

        //    if (Encoding.UTF8.GetString(Base64Url.Decode(clientDataJSON.Challenge)) != activeChallenge)
        //        return BusinessError("INVALID_CHALLENGE");

        //    var attestationObject = CBORObject.DecodeFromBytes(
        //        Base64Url.Decode(HttpUtility.HtmlDecode(webAuthNRequest.Response.AttestationObject))
        //    );

        //    var authData = attestationObject["authData"].GetByteString();
        //    var fmt = attestationObject["fmt"].AsString();

        //    var authDataBytes = authData.AsEnumerable();

        //    var incomingRpIdHash = authDataBytes.Take(32).ToArray(); 
        //    authDataBytes = authDataBytes.Skip(32);

        //    var expectedRp = WorkBench.IsDevelopmentEnvironment ? "localhost" : config.JWTSelfIssuedAudience;

        //    using (var hasher = new SHA256Managed())
        //    {
        //        var rpIdHash = hasher.ComputeHash(Encoding.UTF8.GetBytes(expectedRp));
        //        if (!incomingRpIdHash.SequenceEqual(rpIdHash))
        //            return BusinessError("INVALID_RPID");
        //    }

        //    var flags = new BitArray(authDataBytes.Take(1).ToArray());
        //    authDataBytes = authDataBytes.Skip(1);

        //    var userPresent = flags[0];
        //    // Bit 1 reserved for future use (RFU1)
        //    var userVerified = flags[2]; // (UV)
        //    // Bits 3-5 reserved for future use (RFU2)
        //    var attestedCredentialData = flags[6]; // (AT) "Indicates whether the authenticator added attested credential data"
        //    var extensionDataIncluded = flags[7]; // (ED)

        //    var counter = BitConverter.ToUInt32(authDataBytes.Take(4).ToArray());
        //    authDataBytes = authDataBytes.Skip(4);

        //    if (counter != 0)
        //        return BusinessError("INVALID_COUNTER");

        //    var aaguid = authDataBytes.Take(16);
        //    authDataBytes = authDataBytes.Skip(16);

        //    var credentialIdLengthBuffer = authDataBytes.Take(2).Reverse().ToArray();
        //    authDataBytes = authDataBytes.Skip(2);

        //    var credentialIdLength = BitConverter.ToUInt16(credentialIdLengthBuffer);

        //    var credentialId = authDataBytes.Take(credentialIdLength).ToArray();
        //    authDataBytes = authDataBytes.Skip(credentialIdLength);

        //    var cosePubKey = CBORObject.DecodeFromBytes(authDataBytes.ToArray());
        //    var pubKeyJSON = cosePubKey.ToJSONString();

        //    // https://www.w3.org/TR/webauthn-2/#sctn-encoded-credPubKey-examples
        //    var publicKey = pubKeyJSON.ToJsonDocument());

        //    var alg = publicKey.GetValue("3").ToString();

        //    if (!supportedPublicKeyAlgorithms.Contains(alg))
        //    {
        //        var algs = string.Join(", ", supportedPublicKeyAlgorithms);
        //        throw new LightException($"Public key algorithm {alg} not supported. Use one of [ {algs} ] instead");
        //    }

        //    var credential = new WebAuthN
        //    {
        //        DeviceId = deviceId,
        //        CredentialId = Convert.ToBase64String(credentialId),
        //        PublicKey = pubKeyJSON,
        //        Algorithm = alg,
        //        Counter = 0
        //    };

        //    account.Credentials.WebAuthNChallenge = null;
        //    account.Credentials.WebAuthN.Add(credential);

        //    try 
        //    {
        //       await Repository.UpdateAsync(profile);
        //       AddBusinessInfo("WEBAUTHN_CREDENTIAL_REGISTERED");
        //    }
        //    catch (OptimisticConcurrencyLightException)
        //    {
        //        if (tryNum <= 3)
        //            return await RegisterWebAuthNDevice(deviceId, webAuthNRequest, ++tryNum);
        //        else
        //            throw;
        //    }

        //    return Response(webAuthNRequest);
        //}

        //private static byte[] DeserializeSignature(byte[] s)
        //{
        //    // Thanks to: https://crypto.stackexchange.com/questions/1795/how-can-i-convert-a-der-ecdsa-signature-to-asn-1
        //    using var ms = new MemoryStream(s);
        //    var header = ms.ReadByte(); // marker
        //    var b1 = ms.ReadByte(); // length of remaining bytes

        //    var markerR = ms.ReadByte(); // marker
        //    var b2 = ms.ReadByte(); // length of vr
        //    var vr = new byte[b2]; // signed big-endian encoding of r
        //    ms.Read(vr, 0, vr.Length);
        //    vr = RemoveAnyNegativeFlag(vr); // r

        //    var markerS = ms.ReadByte(); // marker 
        //    var b3 = ms.ReadByte(); // length of vs
        //    var vs = new byte[b3]; // signed big-endian encoding of s
        //    ms.Read(vs, 0, vs.Length);
        //    vs = RemoveAnyNegativeFlag(vs); // s

        //    var parsedSignature = new byte[vr.Length + vs.Length];
        //    vr.CopyTo(parsedSignature, 0);
        //    vs.CopyTo(parsedSignature, vr.Length);

        //    return parsedSignature;
        //}

        //private static byte[] RemoveAnyNegativeFlag(byte[] input)
        //{
        //    if (input[0] != 0) return input;

        //    var output = new byte[input.Length - 1];
        //    Array.Copy(input, 1, output, 0, output.Length);
        //    return output;
        //}

        #endregion
    }
}
