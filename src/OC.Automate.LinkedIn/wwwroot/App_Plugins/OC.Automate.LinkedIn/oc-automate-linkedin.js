import { LitElement, html, css } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";

export class LinkedInAuthorizeButtonElement extends UmbElementMixin(LitElement) {

  static properties = {
    value: { type: String },
    _connectionName: { state: true },
  };

  constructor() {
    super();
    this.value = "";
    this._connectionName = "";
  }

  _onInputChange(e) {
    this._connectionName = e.target.value?.trim() || "";
  }

  _onAuthorizeClick() {
    const name = this._connectionName;
    if (!name) {
      alert("Please enter your Connection Name above, then click Authorize.");
      return;
    }

    const authorizeUrl = `/umbraco/api/linkedin/authorize?connectionName=${encodeURIComponent(name)}`;
    window.open(authorizeUrl, "_blank", "width=600,height=700");
  }

  render() {
    return html`
      <div class="authorize-container">
        <uui-input
          .value=${this._connectionName}
          placeholder="Enter your Connection Name"
          @change=${this._onInputChange}>
        </uui-input>
        <uui-button
          look="primary"
          color="default"
          label="Authorize with LinkedIn"
          @click=${this._onAuthorizeClick}>
          <uui-icon name="icon-link"></uui-icon>
          Authorize with LinkedIn
        </uui-button>
      </div>
    `;
  }

  static styles = css`
    :host {
      display: block;
    }

    .authorize-container {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
  `;
}

if (!customElements.get("linkedin-authorize-button")) {
  customElements.define("linkedin-authorize-button", LinkedInAuthorizeButtonElement);
}
