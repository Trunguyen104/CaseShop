const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const read = (relativePath) => fs.readFileSync(path.join(root, relativePath), 'utf8');

test('chatbot widget is interactive and calls only the application service', () => {
  const widget = read('CaseShop.Web/Components/Shared/Chatbot/ChatbotWidget.razor');

  assert.match(widget, /@rendermode InteractiveServer/);
  assert.match(widget, /@inject IChatbotService ChatbotService/);
  assert.match(widget, /ChatbotService\.GetResponseAsync/);
  assert.doesNotMatch(widget, /AppDbContext|IChatbotKnowledgeRepository/);
  assert.doesNotMatch(widget, /MarkupString/);
});

test('chatbot widget handles form, loading, errors and disabled submission', () => {
  const widget = read('CaseShop.Web/Components/Shared/Chatbot/ChatbotWidget.razor');

  assert.match(widget, /<EditForm[\s\S]*?OnValidSubmit="SendAsync"/);
  assert.match(widget, /<DataAnnotationsValidator/);
  assert.match(widget, /<AppSpinner/);
  assert.match(widget, /<AppButton[\s\S]*?IsLoading="_isSending"/);
  assert.match(widget, /IsDisabled="@string\.IsNullOrWhiteSpace\(_request\.Message\)"/);
  assert.match(widget, /<input[\s\S]*?@bind:event="oninput"/);
  assert.match(widget, /catch \(Exception ex\)/);
  assert.match(widget, /role="alert"/);
});

test('chatbot is responsive and is rendered only by the storefront layout', () => {
  const widget = read('CaseShop.Web/Components/Shared/Chatbot/ChatbotWidget.razor');
  const mainLayout = read('CaseShop.Web/Components/Layout/MainLayout.razor');
  const adminLayout = read('CaseShop.Web/Components/Layout/AdminLayout.razor');

  assert.match(widget, /h-\[82dvh\]/);
  assert.match(widget, /sm:w-\[390px\]/);
  assert.match(widget, /safe-area-inset-bottom/);
  assert.match(mainLayout, /<ChatbotWidget\s*\/>/);
  assert.doesNotMatch(adminLayout, /ChatbotWidget/);
});

test('chatbot component remains focused and below the component size threshold', () => {
  const widget = read('CaseShop.Web/Components/Shared/Chatbot/ChatbotWidget.razor');
  const lineCount = widget.split(/\r?\n/).length;

  assert.ok(lineCount <= 300, `ChatbotWidget has ${lineCount} lines; expected no more than 300`);
  assert.match(widget, /while \(_messages\.Count > 30\)/);
});

test('chatbot auto-scroll interop follows the Blazor render lifecycle', () => {
  const widget = read('CaseShop.Web/Components/Shared/Chatbot/ChatbotWidget.razor');
  const script = read('CaseShop.Web/wwwroot/js/chatbot.js');
  const app = read('CaseShop.Web/Components/App.razor');

  assert.match(widget, /protected override async Task OnAfterRenderAsync/);
  assert.match(widget, /JSRuntime\.InvokeVoidAsync\("caseShopChatbot\.scrollToBottom"/);
  assert.match(widget, /catch \(JSDisconnectedException\)/);
  assert.match(script, /scrollTop = element\.scrollHeight/);
  assert.match(app, /js\/chatbot\.js/);
});
