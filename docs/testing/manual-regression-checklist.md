# Manual Regression Checklist

## Core App
- [ ] App starts
- [ ] Backend health endpoint works
- [ ] Frontend loads without console errors
- [ ] Navigation between main pages works

## Profile
- [ ] Profile can be created/updated
- [ ] Required fields validate correctly
- [ ] Saved data reloads after refresh

## Applications
- [ ] Application can be created
- [ ] Application can be selected
- [ ] Job posting text can be saved
- [ ] Application status updates correctly

## CV Upload / Extraction
- [ ] PDF upload works
- [ ] Invalid file gives clear error
- [ ] Extracted data appears in review state
- [ ] User can accept/import extracted data
- [ ] User can reject/cancel without modifying profile
- [ ] AI/provider failure is handled clearly

## AI Draft Flow
- [ ] Prepare application works
- [ ] Draft generation works
- [ ] Missing profile/job data gives clear guidance
- [ ] Generated draft can be copied/exported