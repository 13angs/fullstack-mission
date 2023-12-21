import { expect, test } from 'vitest'
import { render, screen } from '@testing-library/react'
import Page from '../app/page'; // Adjust the path according to your project structure


test('Chat component', () => {
    render(<Page />)
});
