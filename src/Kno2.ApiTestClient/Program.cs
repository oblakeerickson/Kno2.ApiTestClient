﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Kno2.ApiTestClient.Helpers;
using Kno2.ApiTestClient.Resources;
using Newtonsoft.Json;

namespace Kno2.ApiTestClient
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                // Initialize the configuration data
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                "Initializing Configuration".AsOpeningBanner(ConsoleColor.DarkGray, false);
                ApiConfig apiConfig = new ApiConfig();
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --



                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                // Create a reusable HttpClient
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                "Creating Authenticated Http Client".AsInlineBanner(ConsoleColor.Gray);
                HttpClient httpClient = HttpClientHelper.CreateHttpClient(baseUri: apiConfig.BaseUri,
                    defaultAccept: "application/json",
                    clientId: apiConfig.ClientId,
                    clientSecret: apiConfig.ClientSecret,
                    authUri: apiConfig.AuthUri,
                    grantType: "client_credentials",
                    emrSessionValue: apiConfig.EmrSessionValue
                    );
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --



                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                // Validate the associated addresses
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                string toAddress = "results@" + apiConfig.DirectMessageDomain;
                string fromAddress = "referral@" + apiConfig.DirectMessageDomain;
                "Validating Addresses".AsInlineBanner(dark);
                Dictionary<string, bool> addressValidationResults =
                    ApiHelper.ValidateAddresses(httpClient: httpClient,
                        directoryValidateUri: apiConfig.DirectoryValidate(),
                        addresses: new[] { toAddress, fromAddress });
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --



                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                // Request the available document types
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                "Requesting Available Document Types".AsInlineBanner(light);
                IEnumerable<string> documentTypes = ApiHelper.RequestDocumentTypes(httpClient: httpClient,
                    documentTypesUri: apiConfig.DocumentTypesUri());
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --



                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                // Request a message draft id
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                "Requesting Draft Id".AsInlineBanner(dark);
                var message = ApiHelper.RequestMessageDraft(httpClient: httpClient,
                      messageUri: apiConfig.MessagesUri()
                    );
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --



                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                // Upload an attachment using the draft id
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                var attachmentIds = new List<string>();
                foreach (var fileType in Enum.GetValues(typeof(FileType)).Cast<FileType>())
                {
                    string fileName = FileHelpers.GenerateAttachmentName(fileType);
                    ("Uploading " + fileType + " Attachment " + fileName).AsInlineBanner(light);
                    var attachment = ApiHelper.UploadAttachment(httpClient: httpClient,
                        attachmentsUri: apiConfig.AttachmentsUri(message.Id),
                        fileName: fileName,
                        attachment: new AttachmentResource
                        {
                            NativeFileName = fileName,
                            NativeFileBytes = FileHelpers.GetSampleAttachmentBytes(fileType),
                            DocumentType = documentTypes.First(),
                            AttachmentMetaResource = new AttachmentMetaResource
                            {
                                DocumentTitle = fileType.Description() + " Sample Document Title",
                                DocumentType = documentTypes.First(),
                                DocumentDate = DateTime.UtcNow,
                                DocumentDescription = fileType.Description() + " Sample Document Description",
                                Confidentiality = Confidentiality.Normal
                            }
                        }
                        );
                    // The UploadAttachment helper method and the underlying API will give us the full attachment object
                    //  back but for the sake of the metadata requests we're only storing the Ids at this point
                    attachmentIds.Add(attachment.Id);
                }
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --



                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                // Request the attachment meta data
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                var attachments = new List<AttachmentResource>();
                foreach (var id in attachmentIds)
                {
                    ("Requesting Attachment Metadata for attachment " + id).AsInlineBanner(dark);
                    var metadata = ApiHelper.RequestAttachmentMetadata(httpClient: httpClient,
                        attachmentsUri: apiConfig.AttachmentsUri(messageId: message.Id, attachmentId: id)
                        );
                    attachments.Add(metadata);
                }
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --



                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                // Send the message (draft)
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                "Sending the message (draft)".AsInlineBanner(light);

                message.Attachments = attachments;
                message.Subject = "Referral";
                message.ToAddress = toAddress;
                message.FromAddress = fromAddress;
                message.PatientResource.PatientId = "8675309";
                message.PatientResource.FirstName = "John";
                message.PatientResource.LastName = "Smith (emr-client)";
                message.PatientResource.Gender = "M";
                message.PatientResource.BirthDate = new DateTime(1980, 1, 1).ToShortDateString();

                ApiHelper.SendDraft(httpClient: httpClient,
                    messageUri: apiConfig.MessagesUri(message.Id),
                    messageResource: message);
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --



                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                // Updating the message (draft)
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                "Updating the message (draft)".AsInlineBanner(light);

                message.PatientResource.MiddleName = " (emr-client)";
                message.PatientResource.LastName = "Smith";

                ApiHelper.SendDraft(httpClient: httpClient,
                    messageUri: apiConfig.MessagesUri(message.Id),
                    messageResource: message);
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --



                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                // Send the message (release)
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                "Sending the message (release)".AsInlineBanner(dark);

                message.Comments = "Comments";
                message.ReasonForDisclosure = "Additional disclosure reason ";
                message.Body = "Referral from Caring Hands Village\n\n" +
                                    "Referral for: \nPatient ID: 8675309\n" +
                                    "Patient Name: John Smith\nDOB: 01/01/1980\n\n" +
                                    "Comments:\nComments";

                ApiHelper.SendRelease(httpClient: httpClient,
                    messageSendUri: apiConfig.MessageSendUri(message.Id),
                    messageResource: message
                    );
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --



                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                // Request the unprocessed intake messages
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                "Requesting Available Unprocessed Intake Messages".AsInlineBanner(light);
                ApiHelper.RequestUnprocessedIntakeMessages(httpClient: httpClient,
                    documentsMessagesUri: apiConfig.MessageSearch());
                ConsoleHelper.HeaderLine(true);
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --



                // Message Download / Recieve



                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                // Set the message output to be next the executable
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                string messageOutputPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --



                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                // Request the message
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                "Getting Message".AsInlineBanner(dark);
                string messageJson = ApiHelper.RequestMessage(httpClient: httpClient,
                                                messageUri: apiConfig.MessagesUri(message.Id)
                                                );
                string localMessageDirectory = Path.Combine(messageOutputPath, "MessageDownload", message.Id);
                if (!Directory.Exists(localMessageDirectory))
                    Directory.CreateDirectory(localMessageDirectory);

                File.WriteAllText(Path.Combine(localMessageDirectory, "message.json"), messageJson);
                var retrievedMessage = JsonConvert.DeserializeObject<MessageResource>(messageJson);
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --



                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                // Request the native attachment then save it to disk.
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                foreach (var attachment in message.Attachments.OrderBy(a => a.IsPreviewAvailable))
                {
                    // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                    // Request the native attachment
                    // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                    ("Requesting Native Attachment File Data " + attachment.NativeFileName).AsBanner(light, true, false);
                    byte[] fileBytes = ApiHelper.RequestAttachment(httpClient: httpClient,
                                           attachmentsUri: apiConfig.AttachmentsUri(messageId: retrievedMessage.Id, attachmentId: attachment.Id),
                                           mimeType: "application/octet-stream");
                    attachment.NativeFileBytes = fileBytes;
                    // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                    // Save the file bytes as per the nativeFileName metadata field
                    // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                    if (fileBytes.Length > 0 && FileHelpers.FileHashMatches(attachment))
                    {
                        string fileName = Path.Combine(messageOutputPath, localMessageDirectory, attachment.NativeFileName);
                        (" √ saving file as " + fileName).AsClosingBanner(light);
                        using (var stream = new FileStream(fileName, FileMode.Create))
                            stream.Write(attachment.NativeFileBytes, 0, attachment.NativeFileBytes.Length);
                    }
                    else
                    {
                        (" There was a problem retrieving attachment").AsClosingBanner(light);
                    }
                }
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --



                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                // Request the pdf converted attachment then save it to disk.
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                foreach (var attachment in message.Attachments.OrderBy(a => a.Id))
                {
                    // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                    // Request the native attachment
                    // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                    ("Requesting PDF Converted Attachment File Data " + attachment.PdfFileName).AsBanner(light, true, false);
                    byte[] fileBytes = ApiHelper.RequestAttachment(httpClient: httpClient,
                                           attachmentsUri: apiConfig.AttachmentsUri(messageId: retrievedMessage.Id, attachmentId: attachment.Id),
                                           mimeType: "application/pdf");
                    attachment.PdfFileBytes = fileBytes;
                    // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                    // Save the file bytes as per the nativeFileName metadata field
                    // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                    if (fileBytes.Length > 0)
                    {
                        string fileName = Path.Combine(messageOutputPath, localMessageDirectory, attachment.PdfFileName);
                        (" √ saving file as " + fileName).AsClosingBanner(light);
                        using (var stream = new FileStream(fileName, FileMode.Create))
                            stream.Write(attachment.PdfFileBytes, 0, attachment.PdfFileBytes.Length);
                    }
                    else
                    {
                        (" X looks like the attachment id " + attachment.Id + " didn't convert to pdf")
                            .ToConsole(ConsoleColor.Red);
                        (" There was a problem retrieving converted attachment").AsClosingBanner(light);
                    }
                }
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --



                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                // Send a message read event
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                "Sending Message Read Event".AsBanner(ConsoleColor.DarkGreen, true, false);
                ApiHelper.RequesetMessageReadEvent(httpClient: httpClient,
                            messageReadEventUri: apiConfig.MessageReadEventUri(retrievedMessage.Id),
                            messageId: retrievedMessage.Id,
                            subject: message.Subject
                          );
                ConsoleHelper.HeaderLine(false);
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --



                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                // Send a attachmenet read event for each attachment
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --
                foreach (var attachment in message.Attachments)
                {
                    ("Sending Attachment Read Event for Attachment " + attachment.NativeFileName).AsBanner(ConsoleColor.DarkGreen, true, false);
                    ApiHelper.RequestAttachmentReadEvent(httpClient: httpClient,
                                attachmentReadUri: apiConfig.AttachmentReadUri(messageId: retrievedMessage.Id, attachmentId: attachment.Id)
                              );
                    ConsoleHelper.HeaderLine(false);
                }
                // -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- --



            }
            catch (AggregateException ex)
            {
                "Error".AsOpeningBanner(ConsoleColor.Red);
                foreach (var innerException in ex.InnerExceptions)
                {
                    innerException.Message.ToConsole(ConsoleColor.Red);
                    if (innerException.InnerException != null)
                    {
                        (" - " + innerException.InnerException.Message).ToConsole(ConsoleColor.Red);
                    }
                }
                string.Empty.AsClosingBanner(ConsoleColor.Red);
            }
            catch (Exception ex)
            {
                ex.Message.AsBanner(ConsoleColor.Red);
            }

            Console.ResetColor();
        }

        static ConsoleColor light = ConsoleColor.Cyan;
        static ConsoleColor dark = ConsoleColor.DarkCyan;

    }
}
