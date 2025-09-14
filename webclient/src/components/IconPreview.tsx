import AnthropicIcon from './icons/AnthropicIcon';
import GoogleAIIcon from './icons/GoogleAIIcon';
import OpenAIIcon from './icons/OpenAIIcon';

const IconPreview = () => (
  <div style={{ padding: '20px', display: 'flex', gap: '20px', backgroundColor: 'white' }}>
    <div>
      <p>Anthropic</p>
      <AnthropicIcon style={{ width: '50px', height: '50px' }} />
    </div>
    <div>
      <p>Google AI</p>
      <GoogleAIIcon style={{ width: '50px', height: '50px' }} />
    </div>
    <div>
      <p>OpenAI</p>
      <OpenAIIcon style={{ width: '50px', height: '50px' }} />
    </div>
  </div>
);

export default IconPreview;
