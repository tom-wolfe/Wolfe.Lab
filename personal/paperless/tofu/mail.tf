# Ingests mail in the "Paperless" folder into Paperless through the Proton Bridge.
resource "restapi_object" "proton" {
  path         = "/api/mail_accounts/"
  read_path    = "/api/mail_accounts/{id}/"
  update_path  = "/api/mail_accounts/{id}/"
  destroy_path = "/api/mail_accounts/{id}/"

  data = jsonencode({
    name = "Proton"

    # Bridge on the `lab` network (personal/mail/bridge).
    imap_server = "proton-bridge"
    imap_port   = 143
    # 1 = none. Bridge offers STARTTLS, but with a self-signed certificate
    # issued to 127.0.0.1 alone, and Paperless checks the hostname — so it
    # can never validate as `proton-bridge`, even with
    # PAPERLESS_EMAIL_CERTIFICATE_LOCATION. Bridge accepts a plain login,
    # and the connection never leaves the Docker network.
    imap_security = 1

    username      = var.bridge_username
    password      = var.bridge_password
    is_token      = false
    character_set = "UTF-8"
    account_type  = 1 # IMAP
  })

  # The API reads the password back as asterisks, and adds owner and
  # permission fields of its own.
  ignore_changes_to       = ["password"]
  ignore_server_additions = true
}

resource "restapi_object" "paperless_folder" {
  path         = "/api/mail_rules/"
  read_path    = "/api/mail_rules/{id}/"
  update_path  = "/api/mail_rules/{id}/"
  destroy_path = "/api/mail_rules/{id}/"

  data = jsonencode({
    name    = "Paperless folder"
    account = tonumber(restapi_object.proton.id)
    enabled = true
    order   = 0

    # A Proton folder, which Bridge presents under Folders/. Filing a mail
    # there is the whole interface: no sender or subject filters to keep.
    folder = "Folders/Paperless"
    # 0 = no age limit: a mail filed from years back is still wanted.
    maximum_age = 0

    # 1 = attachments only, for both. Consuming the mail itself needs Tika
    # and Gotenberg, which this service deliberately does not run (README.md).
    # Attachments Paperless cannot read are skipped and logged.
    attachment_type   = 1
    consumption_scope = 1

    # 2 = move, back out to Archive once consumed: the folder empties
    # itself, and read state never matters.
    action           = 2
    action_parameter = "Archive"

    assign_title_from         = 1 # the mail's subject
    assign_correspondent_from = 1 # nothing; the classifier and AI suggest one
    assign_tags               = []
  })

  ignore_server_additions = true
}
